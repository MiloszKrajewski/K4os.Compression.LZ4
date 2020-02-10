// ReSharper disable IdentifierTypo
// ReSharper disable InconsistentNaming
// ReSharper disable AccessToStaticMemberViaDerivedType

using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;

// ReSharper disable ConditionIsAlwaysTrueOrFalse

// ReSharper disable BuiltInTypeReferenceStyle

namespace K4os.Compression.LZ4.Engine
{
	#if BIT32
	using Mem = Internal.Mem32;
	using ptr_t = Int32;
	using size_t = Int32;
	
	internal unsafe class LZ4_32: LZ4_xx
	#else
	using Mem = Internal.Mem64;
	using ptr_t = Int64;
	using size_t = Int32;

	internal unsafe class LZ4_64: LZ4_xx
	#endif
	{
		#if BIT32
		protected const bool BIT32 = true;
		protected const int ARCH_SIZE = 4;
		protected const int HASH_UNIT = 4;

		private static readonly uint[] DeBruijnBytePos = {
			0, 0, 3, 0, 3, 1, 3, 0,
			3, 2, 2, 1, 3, 2, 0, 1,
			3, 3, 1, 2, 2, 2, 2, 0,
			3, 1, 2, 0, 1, 0, 1, 1
		};

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		protected static uint LZ4_NbCommonBytes(uint val) =>
			DeBruijnBytePos[
				unchecked((uint) ((int) val & -(int) val) * 0x077CB531U >> 27)];

		#else

		protected const bool BIT32 = false;
		protected const int ARCH_SIZE = 8;
		protected const int HASH_UNIT = 8;

		private static readonly uint[] DeBruijnBytePos = {
			0, 0, 0, 0, 0, 1, 1, 2,
			0, 3, 1, 3, 1, 4, 2, 7,
			0, 2, 3, 6, 1, 5, 3, 5,
			1, 3, 4, 4, 2, 5, 6, 7,
			7, 0, 1, 2, 3, 3, 4, 6,
			2, 6, 5, 5, 3, 4, 5, 6,
			7, 1, 2, 4, 6, 4, 4, 5,
			7, 2, 6, 5, 7, 6, 7, 7
		};

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		protected static uint LZ4_NbCommonBytes(ulong val) =>
			DeBruijnBytePos[
				unchecked((ulong) ((long) val & -(long) val) * 0x0218A392CDABBD3Ful >> 58)];

		#endif

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		protected static uint LZ4_count(byte* pIn, byte* pMatch, byte* pInLimit)
		{
			const int STEPSIZE = ARCH_SIZE;

			var pStart = pIn;

			if (pIn < pInLimit - (STEPSIZE - 1))
			{
				var diff = Mem.PeekW(pMatch) ^ Mem.PeekW(pIn);
				if (diff != 0)
					return LZ4_NbCommonBytes(diff);

				pIn += STEPSIZE;
				pMatch += STEPSIZE;
			}

			while (pIn < pInLimit - (STEPSIZE - 1))
			{
				var diff = Mem.PeekW(pMatch) ^ Mem.PeekW(pIn);
				if (diff != 0)
					return (uint) (pIn + LZ4_NbCommonBytes(diff) - pStart);

				pIn += STEPSIZE;
				pMatch += STEPSIZE;
			}

			#if !BIT32

			if (pIn < pInLimit - 3 && Mem.Peek4(pMatch) == Mem.Peek4(pIn))
			{
				pIn += 4;
				pMatch += 4;
			}

			#endif

			if (pIn < pInLimit - 1 && Mem.Peek2(pMatch) == Mem.Peek2(pIn))
			{
				pIn += 2;
				pMatch += 2;
			}

			if (pIn < pInLimit && *pMatch == *pIn)
				pIn++;

			return (uint) (pIn - pStart);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static uint LZ4_hashPosition(void* p, tableType_t tableType)
		{
			#if !BIT32
			if (tableType != tableType_t.byU16)
				return LZ4_hash5(Mem.PeekW(p), tableType);
			#endif
			return LZ4_hash4(Mem.Peek4(p), tableType);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		protected static void LZ4_putPosition(
			byte* p, void* tableBase, tableType_t tableType, byte* srcBase) =>
			LZ4_putPositionOnHash(p, LZ4_hashPosition(p, tableType), tableBase, tableType, srcBase);

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		protected static byte* LZ4_getPosition(
			byte* p, void* tableBase, tableType_t tableType, byte* srcBase) =>
			LZ4_getPositionOnHash(LZ4_hashPosition(p, tableType), tableBase, tableType, srcBase);

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static LZ4_stream_t* LZ4_createStream()
		{
			LZ4_stream_t* lz4s = (LZ4_stream_t*) Mem.Alloc(sizeof(LZ4_stream_t));
			LZ4_initStream(lz4s);
			return lz4s;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static LZ4_stream_t* LZ4_initStream(LZ4_stream_t* buffer)
		{
			Mem.Zero((byte*) buffer, sizeof(LZ4_stream_t));
			return buffer;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static int LZ4_freeStream(LZ4_stream_t* LZ4_stream)
		{
			if (LZ4_stream == null) return 0;

			Mem.Free(LZ4_stream);
			return 0;
		}
		
		#region dictionary
		
		protected static void LZ4_renormDictT(LZ4_stream_t* LZ4_dict, int nextSize)
		{
			Debug.Assert(nextSize >= 0);
			if (LZ4_dict->currentOffset + (uint)nextSize > 0x80000000) {   
				/* potential ptrdiff_t overflow (32-bits mode) */
				/* rescale hash table */
				uint delta = LZ4_dict->currentOffset - 64 * KB;
				byte* dictEnd = LZ4_dict->dictionary + LZ4_dict->dictSize;
				int i;
				for (i=0; i<LZ4_HASH_SIZE_U32; i++) {
					if (LZ4_dict->hashTable[i] < delta) LZ4_dict->hashTable[i]=0;
					else LZ4_dict->hashTable[i] -= delta;
				}
				LZ4_dict->currentOffset = 64 * KB;
				if (LZ4_dict->dictSize > 64 * KB) LZ4_dict->dictSize = 64 * KB;
				LZ4_dict->dictionary = dictEnd - LZ4_dict->dictSize;
			}
		}
		
		public int LZ4_loadDict (LZ4_stream_t* LZ4_dict, byte* dictionary, int dictSize)
		{
			//#define HASH_UNIT sizeof(reg_t)
			const int HASH_UNIT = ARCH_SIZE; 
			LZ4_stream_t* dict = LZ4_dict;
			tableType_t tableType = tableType_t.byU32;
			byte* p = (byte*)dictionary;
			byte* dictEnd = p + dictSize;
			byte* @base;

			LZ4_initStream(LZ4_dict);

			dict->currentOffset += 64 * KB;

			if (dictSize < (int)HASH_UNIT) {
				return 0;
			}

			if ((dictEnd - p) > 64 * KB) p = dictEnd - 64 * KB;
			@base = dictEnd - dict->currentOffset;
			dict->dictionary = p;
			dict->dictSize = (uint)(dictEnd - p);
			dict->tableType = tableType;

			while (p <= dictEnd-HASH_UNIT) {
				LZ4_putPosition(p, dict->hashTable, tableType, @base);
				p+=3;
			}

			return (int)dict->dictSize;
		}
		
		public int LZ4_saveDict (LZ4_stream_t* LZ4_dict, byte* safeBuffer, int dictSize)
		{
			LZ4_stream_t* dict = LZ4_dict;
			byte* previousDictEnd = dict->dictionary + dict->dictSize;

			if ((uint)dictSize > 64 * KB) { dictSize = 64 * KB; } /* useless to define a dictionary > 64 KB */
			if ((uint)dictSize > dict->dictSize) { dictSize = (int)dict->dictSize; }

			Mem.Move(safeBuffer, previousDictEnd - dictSize, dictSize);

			dict->dictionary = (byte*)safeBuffer;
			dict->dictSize = (uint)dictSize;

			return dictSize;
		}
		
		#endregion

		#region compress (generic)

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		protected static int LZ4_compress_generic(
			LZ4_stream_t* cctx,
			byte* source,
			byte* dest,
			int inputSize,
			int* inputConsumed, /* only written when outputDirective == fillOutput */
			int maxOutputSize,
			limitedOutput_directive outputDirective,
			tableType_t tableType,
			dict_directive dictDirective,
			dictIssue_directive dictIssue,
			int acceleration)
		{
			int result;
			byte* ip = (byte*) source;

			uint startIndex = cctx->currentOffset;
			byte* @base = (byte*) source - startIndex;
			byte* lowLimit;

			LZ4_stream_t* dictCtx = (LZ4_stream_t*) cctx->dictCtx;
			byte* dictionary =
				dictDirective == dict_directive.usingDictCtx ? dictCtx->dictionary
					: cctx->dictionary;
			uint dictSize =
				dictDirective == dict_directive.usingDictCtx ? dictCtx->dictSize : cctx->dictSize;
			uint dictDelta = (dictDirective == dict_directive.usingDictCtx)
				? startIndex - dictCtx->currentOffset : 0;

			bool maybe_extMem = (dictDirective == dict_directive.usingExtDict)
				|| (dictDirective == dict_directive.usingDictCtx);
			uint prefixIdxLimit = startIndex - dictSize;
			byte* dictEnd = dictionary + dictSize;
			byte* anchor = (byte*) source;
			byte* iend = ip + inputSize;
			byte* mflimitPlusOne = iend - MFLIMIT + 1;
			byte* matchlimit = iend - LASTLITERALS;

			/* the dictCtx currentOffset is indexed on the start of the dictionary,
			 * while a dictionary in the current context precedes the currentOffset */
			byte* dictBase = (dictDirective == dict_directive.usingDictCtx) ?
				dictionary + dictSize - dictCtx->currentOffset :
				dictionary + dictSize - startIndex;

			byte* op = (byte*) dest;
			byte* olimit = op + maxOutputSize;

			uint offset = 0;
			uint forwardH;

			if (outputDirective == limitedOutput_directive.fillOutput && maxOutputSize < 1)
			{
				return 0;
			}

			if ((uint) inputSize > (uint) LZ4_MAX_INPUT_SIZE) { return 0; }

			if ((tableType == tableType_t.byU16) && (inputSize >= LZ4_64Klimit)) { return 0; }

			if (tableType == tableType_t.byPtr)
				Debug.Assert(dictDirective == dict_directive.noDict);
			Debug.Assert(acceleration >= 1);

			lowLimit = (byte*) source
				- (dictDirective == dict_directive.withPrefix64k ? dictSize : 0);

			/* Update context state */
			if (dictDirective == dict_directive.usingDictCtx)
			{
				/* Subsequent linked blocks can't use the dictionary. */
				/* Instead, they use the block we just compressed. */
				cctx->dictCtx = null;
				cctx->dictSize = (uint) inputSize;
			}
			else
			{
				cctx->dictSize += (uint) inputSize;
			}

			cctx->currentOffset += (uint) inputSize;
			cctx->tableType = tableType;

			if (inputSize < LZ4_minLength) goto _last_literals;

			/* First Byte */
			LZ4_putPosition(ip, cctx->hashTable, tableType, @base);
			ip++;
			forwardH = LZ4_hashPosition(ip, tableType);

			/* Main Loop */
			for (;;)
			{
				byte* match;
				byte* token;
				byte* filledIp;

				/* Find a match */
				if (tableType == tableType_t.byPtr)
				{
					byte* forwardIp = ip;
					int step = 1;
					int searchMatchNb = acceleration << LZ4_skipTrigger;
					do
					{
						uint h = forwardH;
						ip = forwardIp;
						forwardIp += step;
						step = (searchMatchNb++ >> LZ4_skipTrigger);

						if ((forwardIp > mflimitPlusOne)) goto _last_literals;

						Debug.Assert(ip < mflimitPlusOne);

						match = LZ4_getPositionOnHash(h, cctx->hashTable, tableType, @base);
						forwardH = LZ4_hashPosition(forwardIp, tableType);
						LZ4_putPositionOnHash(ip, h, cctx->hashTable, tableType, @base);
					}
					while ((match + LZ4_DISTANCE_MAX < ip) || (Mem.Peek4(match) != Mem.Peek4(ip)));
				}
				else
				{
					/* byU32, byU16 */

					byte* forwardIp = ip;
					int step = 1;
					int searchMatchNb = acceleration << LZ4_skipTrigger;
					do
					{
						uint h = forwardH;
						uint current = (uint) (forwardIp - @base);
						uint matchIndex = LZ4_getIndexOnHash(h, cctx->hashTable, tableType);
						Debug.Assert(matchIndex <= current);
						Debug.Assert(forwardIp - @base < (ptr_t) (2 * GB - 1));
						ip = forwardIp;
						forwardIp += step;
						step = (searchMatchNb++ >> LZ4_skipTrigger);

						if ((forwardIp > mflimitPlusOne)) goto _last_literals;

						Debug.Assert(ip < mflimitPlusOne);

						if (dictDirective == dict_directive.usingDictCtx)
						{
							if (matchIndex < startIndex)
							{
								Debug.Assert(tableType == tableType_t.byU32);
								matchIndex = LZ4_getIndexOnHash(
									h, dictCtx->hashTable, tableType_t.byU32);
								match = dictBase + matchIndex;
								matchIndex += dictDelta;
								lowLimit = dictionary;
							}
							else
							{
								match = @base + matchIndex;
								lowLimit = (byte*) source;
							}
						}
						else if (dictDirective == dict_directive.usingExtDict)
						{
							if (matchIndex < startIndex)
							{
								Debug.Assert(startIndex - matchIndex >= MINMATCH);
								match = dictBase + matchIndex;
								lowLimit = dictionary;
							}
							else
							{
								match = @base + matchIndex;
								lowLimit = (byte*) source;
							}
						}
						else
						{
							match = @base + matchIndex;
						}

						forwardH = LZ4_hashPosition(forwardIp, tableType);
						LZ4_putIndexOnHash(current, h, cctx->hashTable, tableType);

						if ((dictIssue == dictIssue_directive.dictSmall)
							&& (matchIndex < prefixIdxLimit)) { continue; }

						Debug.Assert(matchIndex < current);
						if (((tableType != tableType_t.byU16)
								|| (LZ4_DISTANCE_MAX < LZ4_DISTANCE_ABSOLUTE_MAX))
							&& (matchIndex + LZ4_DISTANCE_MAX < current))
						{
							continue;
						}

						Debug.Assert((current - matchIndex) <= LZ4_DISTANCE_MAX);

						if (Mem.Peek4(match) == Mem.Peek4(ip))
						{
							if (maybe_extMem) offset = current - matchIndex;
							break;
						}
					}
					while (true);
				}

				filledIp = ip;
				while (((ip > anchor) & (match > lowLimit)) && ((ip[-1] == match[-1])))
				{
					ip--;
					match--;
				}

				{
					var litLength = (uint) (ip - anchor);
					token = op++;
					if ((outputDirective == limitedOutput_directive.limitedOutput) &&
						((op + litLength + (2 + 1 + LASTLITERALS) + (litLength / 255) > olimit)))
					{
						return 0;
					}

					if ((outputDirective == limitedOutput_directive.fillOutput) &&
						((op + (litLength + 240) / 255 + litLength + 2 + 1 + MFLIMIT - MINMATCH
							> olimit)))
					{
						op--;
						goto _last_literals;
					}

					if (litLength >= RUN_MASK)
					{
						int len = (int) (litLength - RUN_MASK);
						*token = (byte) (RUN_MASK << ML_BITS);
						for (; len >= 255; len -= 255) *op++ = 255;
						*op++ = (byte) len;
					}
					else *token = (byte) (litLength << ML_BITS);

					Mem.WildCopy8(op, anchor, op + litLength);
					op += litLength;
				}

				_next_match:
				/* at this stage, the following variables must be correctly set :
				 * - ip : at start of LZ operation
				 * - match : at start of previous pattern occurence; can be within current prefix, or within extDict
				 * - offset : if maybe_ext_memSegment==1 (constant)
				 * - lowLimit : must be == dictionary to mean "match is within extDict"; must be == source otherwise
				 * - token and *token : position to write 4-bits for match length; higher 4-bits for literal length supposed already written
				 */

				if ((outputDirective == limitedOutput_directive.fillOutput) &&
					(op + 2 + 1 + MFLIMIT - MINMATCH > olimit))
				{
					/* the match was too close to the end, rewind and go to last literals */
					op = token;
					goto _last_literals;
				}

				/* Encode Offset */
				if (maybe_extMem)
				{
					/* static test */
					Debug.Assert(offset <= LZ4_DISTANCE_MAX && offset > 0);
					Mem.Poke2(op, (ushort) offset);
					op += 2;
				}
				else
				{
					Debug.Assert(ip - match <= LZ4_DISTANCE_MAX);
					Mem.Poke2(op, (ushort) (ip - match));
					op += 2;
				}

				/* Encode MatchLength */
				{
					uint matchCode;

					if ((dictDirective == dict_directive.usingExtDict
							|| dictDirective == dict_directive.usingDictCtx)
						&& (lowLimit == dictionary) /* match within extDict */)
					{
						byte* limit = ip + (dictEnd - match);
						Debug.Assert(dictEnd > match);
						if (limit > matchlimit) limit = matchlimit;
						matchCode = LZ4_count(ip + MINMATCH, match + MINMATCH, limit);
						ip += (uint) matchCode + MINMATCH;
						if (ip == limit)
						{
							uint more = LZ4_count(limit, (byte*) source, matchlimit);
							matchCode += more;
							ip += more;
						}
					}
					else
					{
						matchCode = LZ4_count(ip + MINMATCH, match + MINMATCH, matchlimit);
						ip += (uint) matchCode + MINMATCH;
					}

					if ((outputDirective != 0)
						&& ((op + (1 + LASTLITERALS) + (matchCode + 240) / 255 > olimit)))
					{
						if (outputDirective == limitedOutput_directive.fillOutput)
						{
							/* Match description too long : reduce it */
							uint newMatchCode =
								15 - 1 + ((uint) (olimit - op) - 1 - LASTLITERALS) * 255;
							ip -= matchCode - newMatchCode;
							Debug.Assert(newMatchCode < matchCode);
							matchCode = newMatchCode;
							if ((ip <= filledIp))
							{
								/* We have already filled up to filledIp so if ip ends up less than filledIp
								 * we have positions in the hash table beyond the current position. This is
								 * a problem if we reuse the hash table. So we have to remove these positions
								 * from the hash table.
								 */
								byte* ptr;
								for (ptr = ip; ptr <= filledIp; ++ptr)
								{
									uint h = LZ4_hashPosition(ptr, tableType);
									LZ4_clearHash(h, cctx->hashTable, tableType);
								}
							}
						}
						else
						{
							Debug.Assert(outputDirective == limitedOutput_directive.limitedOutput);
							return 0;
						}
					}

					if (matchCode >= ML_MASK)
					{
						*token += (byte) ML_MASK; //!!!
						matchCode -= ML_MASK;
						Mem.Poke4(op, 0xFFFFFFFF);
						while (matchCode >= 4 * 255)
						{
							op += 4;
							Mem.Poke4(op, 0xFFFFFFFF);
							matchCode -= 4 * 255;
						}

						op += matchCode / 255;
						*op++ = (byte) (matchCode % 255);
					}
					else
						*token += (byte) (matchCode);
				}
				/* Ensure we have enough space for the last literals. */
				Debug.Assert(
					!(outputDirective == limitedOutput_directive.fillOutput
						&& op + 1 + LASTLITERALS > olimit));

				anchor = ip;

				/* Test end of chunk */
				if (ip >= mflimitPlusOne) break;

				/* Fill table */
				LZ4_putPosition(ip - 2, cctx->hashTable, tableType, @base);

				/* Test next position */
				if (tableType == tableType_t.byPtr)
				{
					match = LZ4_getPosition(ip, cctx->hashTable, tableType, @base);
					LZ4_putPosition(ip, cctx->hashTable, tableType, @base);
					if ((match + LZ4_DISTANCE_MAX >= ip) && (Mem.Peek4(match) == Mem.Peek4(ip)))
					{
						token = op++;
						*token = 0;
						goto _next_match;
					}
				}
				else
				{
					/* byU32, byU16 */

					uint h = LZ4_hashPosition(ip, tableType);
					uint current = (uint) (ip - @base);
					uint matchIndex = LZ4_getIndexOnHash(h, cctx->hashTable, tableType);
					Debug.Assert(matchIndex < current);
					if (dictDirective == dict_directive.usingDictCtx)
					{
						if (matchIndex < startIndex)
						{
							matchIndex = LZ4_getIndexOnHash(
								h, dictCtx->hashTable, tableType_t.byU32);
							match = dictBase + matchIndex;
							lowLimit = dictionary;
							matchIndex += dictDelta;
						}
						else
						{
							match = @base + matchIndex;
							lowLimit = (byte*) source;
						}
					}
					else if (dictDirective == dict_directive.usingExtDict)
					{
						if (matchIndex < startIndex)
						{
							match = dictBase + matchIndex;
							lowLimit = dictionary;
						}
						else
						{
							match = @base + matchIndex;
							lowLimit = (byte*) source;
						}
					}
					else
					{
						match = @base + matchIndex;
					}

					LZ4_putIndexOnHash(current, h, cctx->hashTable, tableType);
					Debug.Assert(matchIndex < current);
					if (((dictIssue != dictIssue_directive.dictSmall)
							|| (matchIndex >= prefixIdxLimit))
						&& (((tableType == tableType_t.byU16)
								&& (LZ4_DISTANCE_MAX == LZ4_DISTANCE_ABSOLUTE_MAX))
							|| (matchIndex + LZ4_DISTANCE_MAX >= current))
						&& (Mem.Peek4(match) == Mem.Peek4(ip)))
					{
						token = op++;
						*token = 0;
						if (maybe_extMem) offset = current - matchIndex;
						goto _next_match;
					}
				}

				forwardH = LZ4_hashPosition(++ip, tableType);
			}

			_last_literals:
			{
				var lastRun = (size_t) (iend - anchor);
				if ((outputDirective != 0) &&
					(op + lastRun + 1 + ((lastRun + 255 - RUN_MASK) / 255) > olimit))
				{
					if (outputDirective == limitedOutput_directive.fillOutput)
					{
						Debug.Assert(olimit >= op);
						lastRun = (size_t) (olimit - op) - 1;
						lastRun -= (lastRun + 240) / 255;
					}
					else
					{
						Debug.Assert(outputDirective == limitedOutput_directive.limitedOutput);
						return 0;
					}
				}

				if (lastRun >= RUN_MASK)
				{
					var accumulator = (size_t) (lastRun - RUN_MASK);
					*op++ = (byte) (RUN_MASK << ML_BITS);
					for (; accumulator >= 255; accumulator -= 255) *op++ = 255;
					*op++ = (byte) accumulator;
				}
				else
				{
					*op++ = (byte) (lastRun << ML_BITS);
				}

				Mem.Copy(op, anchor, lastRun);
				ip = anchor + lastRun;
				op += lastRun;
			}

			if (outputDirective == limitedOutput_directive.fillOutput)
			{
				*inputConsumed = (int) (((byte*) ip) - source);
			}

			result = (int) (((byte*) op) - dest);
			Debug.Assert(result > 0);
			return result;
		}

		#endregion

		public static int LZ4_compress_fast_extState(
			LZ4_stream_t* state, byte* source, byte* dest, int inputSize, int maxOutputSize,
			int acceleration)
		{
			LZ4_stream_t* ctx = LZ4_initStream(state);
			Debug.Assert(ctx != null);
			if (acceleration < 1) acceleration = ACCELERATION_DEFAULT;
			if (maxOutputSize >= LZ4_compressBound(inputSize))
			{
				if (inputSize < LZ4_64Klimit)
				{
					return LZ4_compress_generic(
						ctx, source, dest,
						inputSize, null, 0, limitedOutput_directive.notLimited,
						tableType_t.byU16, dict_directive.noDict, dictIssue_directive.noDictIssue,
						acceleration);
				}
				else
				{
					tableType_t tableType =
						((sizeof(void*) == 4) && ((ptr_t) source > LZ4_DISTANCE_MAX))
							? tableType_t.byPtr : tableType_t.byU32;
					return LZ4_compress_generic(
						ctx, source, dest,
						inputSize, null, 0, limitedOutput_directive.notLimited,
						tableType, dict_directive.noDict, dictIssue_directive.noDictIssue,
						acceleration);
				}
			}
			else
			{
				if (inputSize < LZ4_64Klimit)
				{
					return LZ4_compress_generic(
						ctx, source, dest,
						inputSize, null, maxOutputSize, limitedOutput_directive.limitedOutput,
						tableType_t.byU16, dict_directive.noDict, dictIssue_directive.noDictIssue,
						acceleration);
				}
				else
				{
					tableType_t tableType =
						((sizeof(void*) == 4) && ((ptr_t) source > LZ4_DISTANCE_MAX))
							? tableType_t.byPtr : tableType_t.byU32;
					return LZ4_compress_generic(
						ctx, source, dest,
						inputSize, null, maxOutputSize, limitedOutput_directive.limitedOutput,
						tableType, dict_directive.noDict, dictIssue_directive.noDictIssue,
						acceleration);
				}
			}
		}

		public static int LZ4_compress_fast(
			byte* source, byte* dest, int inputSize, int maxOutputSize, int acceleration)
		{
			LZ4_stream_t ctx;
			return LZ4_compress_fast_extState(
				&ctx, source, dest, inputSize, maxOutputSize, acceleration);
		}

		public static int LZ4_compress_default(
			byte* src, byte* dst, int srcSize, int maxOutputSize) =>
			LZ4_compress_fast(src, dst, srcSize, maxOutputSize, 1);

		public static int LZ4_compress_fast_continue(
			LZ4_stream_t* LZ4_stream,
			byte* source, byte* dest,
			int inputSize, int maxOutputSize,
			int acceleration)
		{
			tableType_t tableType = tableType_t.byU32;
			LZ4_stream_t* streamPtr = LZ4_stream;
			byte* dictEnd = streamPtr->dictionary + streamPtr->dictSize;

			if (streamPtr->dirty) { return 0; } /* Uninitialized structure detected */

			LZ4_renormDictT(streamPtr, inputSize); /* avoid index overflow */
			if (acceleration < 1) acceleration = ACCELERATION_DEFAULT;

			/* invalidate tiny dictionaries */
			if ((streamPtr->dictSize - 1 < 4 - 1) /* intentional underflow */
				&& (dictEnd != (byte*) source))
			{
				streamPtr->dictSize = 0;
				streamPtr->dictionary = (byte*) source;
				dictEnd = (byte*) source;
			}

			/* Check overlapping input/dictionary space */
			{
				byte* sourceEnd = (byte*) source + inputSize;
				if ((sourceEnd > streamPtr->dictionary) && (sourceEnd < dictEnd))
				{
					streamPtr->dictSize = (uint) (dictEnd - sourceEnd);
					if (streamPtr->dictSize > 64 * KB) streamPtr->dictSize = 64 * KB;
					if (streamPtr->dictSize < 4) streamPtr->dictSize = 0;
					streamPtr->dictionary = dictEnd - streamPtr->dictSize;
				}
			}

			/* prefix mode : source data follows dictionary */
			if (dictEnd == (byte*) source)
			{
				if ((streamPtr->dictSize < 64 * KB)
					&& (streamPtr->dictSize < streamPtr->currentOffset))
					return LZ4_compress_generic(
						streamPtr, source, dest, inputSize, null, maxOutputSize,
						limitedOutput_directive.limitedOutput, tableType,
						dict_directive.withPrefix64k, dictIssue_directive.dictSmall, acceleration);
				else
					return LZ4_compress_generic(
						streamPtr, source, dest, inputSize, null, maxOutputSize,
						limitedOutput_directive.limitedOutput, tableType,
						dict_directive.withPrefix64k, dictIssue_directive.noDictIssue,
						acceleration);
			}

			/* external dictionary mode */
			{
				int result;
				if (streamPtr->dictCtx != null)
				{
					if (inputSize > 4 * KB) {
						/* For compressing large blobs, it is faster to pay the setup
						 * cost to copy the dictionary's tables into the active context,
						 * so that the compression loop is only looking into one table.
						 */
						Mem.Copy((byte*)streamPtr, (byte*)streamPtr->dictCtx, sizeof(LZ4_stream_t));
						result = LZ4_compress_generic(
							streamPtr, source, dest, inputSize, null, maxOutputSize, limitedOutput_directive.limitedOutput,
							tableType, dict_directive.usingExtDict, dictIssue_directive.noDictIssue, acceleration);
					} else {
						result = LZ4_compress_generic(
							streamPtr, source, dest, inputSize, null, maxOutputSize, limitedOutput_directive.limitedOutput,
							tableType, dict_directive.usingDictCtx, dictIssue_directive.noDictIssue, acceleration);
					}
				}
				else
				{
					if ((streamPtr->dictSize < 64 * KB) && (streamPtr->dictSize < streamPtr->currentOffset)) {
						result = LZ4_compress_generic(
							streamPtr, source, dest, inputSize, null, maxOutputSize, limitedOutput_directive.limitedOutput,
							tableType, dict_directive.usingExtDict, dictIssue_directive.dictSmall, acceleration);
					} else {
						result = LZ4_compress_generic(
							streamPtr, source, dest, inputSize, null, maxOutputSize, limitedOutput_directive.limitedOutput,
							tableType, dict_directive.usingExtDict, dictIssue_directive.noDictIssue, acceleration);
					}
				}

				streamPtr->dictionary = (byte*) source;
				streamPtr->dictSize = (uint) inputSize;
				return result;
			}
		}
	}
}
