using System.Runtime.CompilerServices;
using K4os.Compression.LZ4.Internal;

// ReSharper disable InconsistentNaming

namespace K4os.Compression.LZ4.Engine
{
#if BIT32
	internal unsafe class LZ4_32: LZ4_xx
	{
		protected const int ARCH_SIZE = 4;
		protected const int STEPSIZE = 4;
		protected const int HASH_UNIT = 4;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		protected static uint LZ4_read_ARCH(void* p) => *(uint*) p;

		private static readonly uint[] DeBruijnBytePos = {
			0, 0, 3, 0, 3, 1, 3, 0,
			3, 2, 2, 1, 3, 2, 0, 1,
			3, 3, 1, 2, 2, 2, 2, 0,
			3, 1, 2, 0, 1, 0, 1, 1
		};

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		protected static uint LZ4_NbCommonBytes(uint val) =>
			DeBruijnBytePos[(uint) ((int) val & -(int) val) * 0x077CB531U >> 27];

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static tableType_t LZ4_tableType(int inputSize) =>
			inputSize < LZ4_64Klimit ? tableType_t.byU16 :
			sizeof(byte*) == sizeof(uint) ? tableType_t.byPtr :
			tableType_t.byU32;
#else
	internal unsafe class LZ4_64: LZ4_xx
	{
		protected const int ARCH_SIZE = 8;
		protected const int STEPSIZE = 8;
		protected const int HASH_UNIT = 8;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		protected static ulong LZ4_read_ARCH(void* p) => *(ulong*) p;

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
			DeBruijnBytePos[unchecked ((ulong) ((long) val & -(long) val) * 0x0218A392CDABBD3Ful >> 58)];

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static tableType_t LZ4_tableType(int inputSize) =>
			inputSize < LZ4_64Klimit ? tableType_t.byU16 : tableType_t.byU32;
#endif

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		protected static uint LZ4_count(byte* pIn, byte* pMatch, byte* pInLimit)
		{
			var pStart = pIn;

			if (pIn < pInLimit - (STEPSIZE - 1))
			{
				var diff = LZ4_read_ARCH(pMatch) ^ LZ4_read_ARCH(pIn);
				if (diff != 0)
					return LZ4_NbCommonBytes(diff);

				pIn += STEPSIZE;
				pMatch += STEPSIZE;
			}

			while (pIn < pInLimit - (STEPSIZE - 1))
			{
				var diff = LZ4_read_ARCH(pMatch) ^ LZ4_read_ARCH(pIn);
				if (diff != 0)
					return (uint) (pIn + LZ4_NbCommonBytes(diff) - pStart);

				pIn += STEPSIZE;
				pMatch += STEPSIZE;
			}

#if !BIT32
			if (pIn < pInLimit - 3 && Mem.Peek32(pMatch) == Mem.Peek32(pIn))
			{
				pIn += 4;
				pMatch += 4;
			}
#endif

			if (pIn < pInLimit - 1 && Mem.Peek16(pMatch) == Mem.Peek16(pIn))
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
				return LZ4_hash5(LZ4_read_ARCH(p), tableType);
#endif
			return LZ4_hash4(Mem.Peek32(p), tableType);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static void LZ4_putPosition(
			byte* p, void* tableBase, tableType_t tableType, byte* srcBase) =>
			LZ4_putPositionOnHash(p, LZ4_hashPosition(p, tableType), tableBase, tableType, srcBase);

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		static byte* LZ4_getPosition(byte* p, void* tableBase, tableType_t tableType, byte* srcBase) =>
			LZ4_getPositionOnHash(LZ4_hashPosition(p, tableType), tableBase, tableType, srcBase);

		public static int LZ4_compress_generic(
			LZ4_stream_t* cctx,
			byte* source,
			byte* dest,
			int inputSize,
			int maxOutputSize,
			limitedOutput_directive outputLimited,
			tableType_t tableType,
			dict_directive dict,
			dictIssue_directive dictIssue,
			uint acceleration)
		{
			var ip = source;
			byte* ibase;
			byte* lowLimit;
			var lowRefLimit = ip - cctx->dictSize;
			var dictionary = cctx->dictionary;
			var dictEnd = dictionary + cctx->dictSize;
			var dictDelta = dictEnd - source;
			var anchor = source;
			var iend = ip + inputSize;
			var mflimit = iend - MFLIMIT;
			var matchlimit = iend - LASTLITERALS;

			var op = dest;
			var olimit = op + maxOutputSize;

			if (inputSize > LZ4_MAX_INPUT_SIZE)
				return 0;

			switch (dict)
			{
				case dict_directive.withPrefix64k:
					ibase = source - cctx->currentOffset;
					lowLimit = source - cctx->dictSize;
					break;
				case dict_directive.usingExtDict:
					ibase = source - cctx->currentOffset;
					lowLimit = source;
					break;
				default:
					ibase = source;
					lowLimit = source;
					break;
			}

			if (tableType == tableType_t.byU16 && inputSize >= LZ4_64Klimit)
				return 0;

			if (inputSize < LZ4_minLength)
				goto _last_literals;

			LZ4_putPosition(ip, cctx->hashTable, tableType, ibase);
			ip++;
			var forwardH = LZ4_hashPosition(ip, tableType);

			for (;;)
			{
				var refDelta = 0L;
				byte* match;
				byte* token;

				{
					var forwardIp = ip;
					var step = 1u;
					var searchMatchNb = acceleration << LZ4_skipTrigger;
					do
					{
						var h = forwardH;
						ip = forwardIp;
						forwardIp += step;
						step = searchMatchNb++ >> LZ4_skipTrigger;

						if (forwardIp > mflimit)
							goto _last_literals;

						match = LZ4_getPositionOnHash(h, cctx->hashTable, tableType, ibase);
						if (dict == dict_directive.usingExtDict)
						{
							if (match < source)
							{
								refDelta = dictDelta;
								lowLimit = dictionary;
							}
							else
							{
								refDelta = 0;
								lowLimit = source;
							}
						}

						forwardH = LZ4_hashPosition(forwardIp, tableType);
						LZ4_putPositionOnHash(ip, h, cctx->hashTable, tableType, ibase);
					}
					while (
						dictIssue == dictIssue_directive.dictSmall && match < lowRefLimit
						|| tableType != tableType_t.byU16 && match + MAX_DISTANCE < ip
						|| Mem.Peek32(match + refDelta) != Mem.Peek32(ip));
				}

				while (ip > anchor && match + refDelta > lowLimit && ip[-1] == match[refDelta - 1])
				{
					ip--;
					match--;
				}

				{
					var litLength = (uint) (ip - anchor);
					token = op++;
					if (outputLimited == limitedOutput_directive.limitedOutput
						&& op + litLength + (2 + 1 + LASTLITERALS) + litLength / 255 > olimit)
						return 0;

					if (litLength >= RUN_MASK)
					{
						var len = (int) (litLength - RUN_MASK);

						*token = (byte) (RUN_MASK << ML_BITS);
						for (; len >= 255; len -= 255) *op++ = 255;

						*op++ = (byte) len;
					}
					else
					{
						*token = (byte) (litLength << ML_BITS);
					}

					Mem.WildCopy(op, anchor, op + litLength);
					op += litLength;
				}

				_next_match:
				Mem.Poke16(op, (ushort) (ip - match));
				op += 2;

				{
					uint matchCode;

					if (dict == dict_directive.usingExtDict && lowLimit == dictionary)
					{
						match += refDelta;
						var limit = ip + (dictEnd - match);
						if (limit > matchlimit) limit = matchlimit;
						matchCode = LZ4_count(ip + MINMATCH, match + MINMATCH, limit);
						ip += MINMATCH + matchCode;
						if (ip == limit)
						{
							var more = LZ4_count(ip, source, matchlimit);
							matchCode += more;
							ip += more;
						}
					}
					else
					{
						matchCode = LZ4_count(ip + MINMATCH, match + MINMATCH, matchlimit);
						ip += MINMATCH + matchCode;
					}

					if (outputLimited == limitedOutput_directive.limitedOutput
						&& op + (1 + LASTLITERALS) + (matchCode >> 8) > olimit)
						return 0;

					if (matchCode >= ML_MASK)
					{
						*token += (byte) ML_MASK;
						matchCode -= ML_MASK;
						Mem.Poke32(op, 0xFFFFFFFF);
						while (matchCode >= 4 * 255)
						{
							op += 4;
							Mem.Poke32(op, 0xFFFFFFFF);
							matchCode -= 4 * 255;
						}

						op += matchCode / 255;

						*op++ = (byte) (matchCode % 255);
					}
					else
					{
						*token += (byte) matchCode;
					}
				}

				anchor = ip;

				if (ip > mflimit) break;

				LZ4_putPosition(ip - 2, cctx->hashTable, tableType, ibase);

				match = LZ4_getPosition(ip, cctx->hashTable, tableType, ibase);
				if (dict == dict_directive.usingExtDict)
				{
					if (match < source)
					{
						refDelta = dictDelta;
						lowLimit = dictionary;
					}
					else
					{
						refDelta = 0;
						lowLimit = source;
					}
				}

				LZ4_putPosition(ip, cctx->hashTable, tableType, ibase);
				if ((dictIssue != dictIssue_directive.dictSmall || match >= lowRefLimit)
					&& match + MAX_DISTANCE >= ip
					&& Mem.Peek32(match + refDelta) == Mem.Peek32(ip))
				{
					token = op++;
					*token = 0;
					goto _next_match;
				}

				forwardH = LZ4_hashPosition(++ip, tableType);
			}

			_last_literals:
			{
				var lastRun = (int) (iend - anchor);
				if (outputLimited == limitedOutput_directive.limitedOutput
					&& op - dest + lastRun + 1 + (lastRun + 255 - RUN_MASK) / 255 > (uint) maxOutputSize)
					return 0;

				if (lastRun >= RUN_MASK)
				{
					var accumulator = (int) (lastRun - RUN_MASK);

					*op++ = (byte) (RUN_MASK << ML_BITS);
					for (; accumulator >= 255; accumulator -= 255) *op++ = 255;
					*op++ = (byte) accumulator;
				}
				else
				{
					*op++ = (byte) (lastRun << ML_BITS);
				}

				Mem.Copy(op, anchor, lastRun);
				op += lastRun;
			}

			return (int) (op - dest);
		}

		public static int LZ4_compress_fast_extState(
			LZ4_stream_t* state, byte* source, byte* dest, int inputSize, int maxOutputSize,
			int acceleration)
		{
			LZ4_resetStream(state);
			if (acceleration < 1) acceleration = ACCELERATION_DEFAULT;

			var limited =
				maxOutputSize >= LZ4_compressBound(inputSize)
					? limitedOutput_directive.noLimit
					: limitedOutput_directive.limitedOutput;

			return LZ4_compress_generic(
				state,
				source,
				dest,
				inputSize,
				limited == limitedOutput_directive.noLimit ? 0 : maxOutputSize,
				limited,
				LZ4_tableType(inputSize),
				dict_directive.noDict,
				dictIssue_directive.noDictIssue,
				(uint) acceleration);
		}

		public static void LZ4_resetStream(LZ4_stream_t* state) =>
			Mem.Zero((byte*) state, sizeof(LZ4_stream_t));

		public static int LZ4_compress_fast(
			byte* source, byte* dest, int inputSize, int maxOutputSize, int acceleration)
		{
			LZ4_stream_t ctx;
			return LZ4_compress_fast_extState(&ctx, source, dest, inputSize, maxOutputSize, acceleration);
		}

		public static int LZ4_compress_default(
			byte* source, byte* dest, int inputSize, int maxOutputSize) =>
			LZ4_compress_fast(source, dest, inputSize, maxOutputSize, 1);

		static int LZ4_compress_destSize_generic(
			LZ4_stream_t* ctx, byte* src, byte* dst, int* srcSizePtr, int targetDstSize,
			tableType_t tableType)
		{
			var ip = src;
			var base_ = src;
			var lowLimit = src;
			var anchor = ip;
			var iend = ip + *srcSizePtr;
			var mflimit = iend - MFLIMIT;
			var matchlimit = iend - LASTLITERALS;

			var op = dst;
			var oend = op + targetDstSize;
			var oMaxLit = op + targetDstSize - 2 - 8 - 1;
			var oMaxMatch = op + targetDstSize - (LASTLITERALS + 1);
			var oMaxSeq = oMaxLit - 1;

			if (targetDstSize < 1)
				return 0;
			if (*srcSizePtr > LZ4_MAX_INPUT_SIZE)
				return 0;
			if (tableType == tableType_t.byU16 && *srcSizePtr >= LZ4_64Klimit)
				return 0;

			if (*srcSizePtr < LZ4_minLength)
				goto _last_literals; /* Input too small, no compression (all literals) */

			*srcSizePtr = 0;
			LZ4_putPosition(ip, ctx->hashTable, tableType, base_);
			ip++;
			var forwardH = LZ4_hashPosition(ip, tableType);

			for (;;)
			{
				byte* match;
				byte* token;

				{
					var forwardIp = ip;
					var step = 1u;
					var searchMatchNb = 1u << LZ4_skipTrigger;

					do
					{
						var h = forwardH;
						ip = forwardIp;
						forwardIp += step;
						step = searchMatchNb++ >> LZ4_skipTrigger;

						if (forwardIp > mflimit) goto _last_literals;

						match = LZ4_getPositionOnHash(h, ctx->hashTable, tableType, base_);
						forwardH = LZ4_hashPosition(forwardIp, tableType);
						LZ4_putPositionOnHash(ip, h, ctx->hashTable, tableType, base_);
					}
					while (
						tableType != tableType_t.byU16 && match + MAX_DISTANCE < ip
						|| Mem.Peek32(match) != Mem.Peek32(ip));
				}

				while (ip > anchor && match > lowLimit && ip[-1] == match[-1])
				{
					ip--;
					match--;
				}

				{
					var litLength = (uint) (ip - anchor);
					token = op++;
					if (op + (litLength + 240) / 255 + litLength > oMaxLit)
					{
						op--;
						goto _last_literals;
					}

					if (litLength >= RUN_MASK)
					{
						var len = litLength - RUN_MASK;

						*token = (byte) (RUN_MASK << ML_BITS);
						for (; len >= 255; len -= 255) *op++ = 255;

						*op++ = (byte) len;
					}
					else
					{
						*token = (byte) (litLength << ML_BITS);
					}

					Mem.WildCopy(op, anchor, op + litLength);
					op += litLength;
				}

				_next_match:
				Mem.Poke16(op, (ushort) (ip - match));
				op += 2;

				{
					var matchLength = (int) LZ4_count(ip + MINMATCH, match + MINMATCH, matchlimit);

					if (op + (matchLength + 240) / 255 > oMaxMatch)
					{
						matchLength = (int) (15 - 1 + (oMaxMatch - op) * 255);
					}

					ip += MINMATCH + matchLength;

					if (matchLength >= ML_MASK)
					{
						*token += (byte) ML_MASK;
						matchLength -= (int) ML_MASK;
						while (matchLength >= 255)
						{
							matchLength -= 255;
							*op++ = 255;
						}

						*op++ = (byte) matchLength;
					}
					else
					{
						*token += (byte) matchLength;
					}
				}

				anchor = ip;

				if (ip > mflimit) break;
				if (op > oMaxSeq) break;

				LZ4_putPosition(ip - 2, ctx->hashTable, tableType, base_);

				match = LZ4_getPosition(ip, ctx->hashTable, tableType, base_);
				LZ4_putPosition(ip, ctx->hashTable, tableType, base_);
				if (match + MAX_DISTANCE >= ip && Mem.Peek32(match) == Mem.Peek32(ip))
				{
					token = op++;
					*token = 0;
					goto _next_match;
				}

				forwardH = LZ4_hashPosition(++ip, tableType);
			}

			_last_literals:
			{
				var lastRunSize = (int) (iend - anchor);
				if (op + 1 + (lastRunSize + 240) / 255 + lastRunSize > oend)
				{
					lastRunSize = (int) (oend - op) - 1;
					lastRunSize -= (lastRunSize + 240) / 255;
				}

				ip = anchor + lastRunSize;

				if (lastRunSize >= RUN_MASK)
				{
					var accumulator = lastRunSize - RUN_MASK;

					*op++ = (byte) (RUN_MASK << ML_BITS);
					for (; accumulator >= 255; accumulator -= 255) *op++ = 255;

					*op++ = (byte) accumulator;
				}
				else
				{
					*op++ = (byte) (lastRunSize << ML_BITS);
				}

				Mem.Copy(op, anchor, lastRunSize);
				op += lastRunSize;
			}

			*srcSizePtr = (int) (ip - src);
			return (int) (op - dst);
		}

		public static int LZ4_compress_destSize_extState(
			LZ4_stream_t* state, byte* src, byte* dst, int* srcSizePtr, int targetDstSize)
		{
			LZ4_resetStream(state);
			return targetDstSize >= LZ4_compressBound(*srcSizePtr)
				? LZ4_compress_fast_extState(
					state,
					src,
					dst,
					*srcSizePtr,
					targetDstSize,
					1)
				: LZ4_compress_destSize_generic(
					state,
					src,
					dst,
					srcSizePtr,
					targetDstSize,
					LZ4_tableType(*srcSizePtr));
		}

		public static int LZ4_compress_destSize(byte* src, byte* dst, int* srcSizePtr, int targetDstSize)
		{
			LZ4_stream_t ctxBody;
			return LZ4_compress_destSize_extState(&ctxBody, src, dst, srcSizePtr, targetDstSize);
		}

		//--------------------------------------------------------------------

		public static int LZ4_loadDict(LZ4_stream_t* LZ4_dict, byte* dictionary, int dictSize)
		{
			var dict = LZ4_dict;
			var p = dictionary;
			var dictEnd = p + dictSize;

			if (dict->initCheck != 0 || dict->currentOffset > 1 * GB)
				LZ4_resetStream(LZ4_dict);

			if (dictSize < HASH_UNIT)
			{
				dict->dictionary = null;
				dict->dictSize = 0;
				return 0;
			}

			if (dictEnd - p > 64 * KB) p = dictEnd - 64 * KB;
			dict->currentOffset += 64 * KB;
			var base_ = p - dict->currentOffset;
			dict->dictionary = p;
			dict->dictSize = (uint) (dictEnd - p);
			dict->currentOffset += dict->dictSize;

			while (p <= dictEnd - HASH_UNIT)
			{
				LZ4_putPosition(p, dict->hashTable, tableType_t.byU32, base_);
				p += 3;
			}

			return (int) dict->dictSize;
		}

		public static int LZ4_compress_fast_continue(
			LZ4_stream_t* streamPtr, byte* source, byte* dest, int inputSize, int maxOutputSize,
			int acceleration)
		{
			var dictEnd = streamPtr->dictionary + streamPtr->dictSize;

			var smallest = source;
			if (streamPtr->initCheck != 0) return 0; /* Uninitialized structure detected */

			if (streamPtr->dictSize > 0 && smallest > dictEnd) smallest = dictEnd;
			LZ4_renormDictT(streamPtr, smallest);
			if (acceleration < 1) acceleration = ACCELERATION_DEFAULT;

			/* Check overlapping input/dictionary space */
			{
				var sourceEnd = source + inputSize;
				if (sourceEnd > streamPtr->dictionary && sourceEnd < dictEnd)
				{
					streamPtr->dictSize = (uint) (dictEnd - sourceEnd);
					if (streamPtr->dictSize > 64 * KB) streamPtr->dictSize = 64 * KB;
					if (streamPtr->dictSize < 4) streamPtr->dictSize = 0;
					streamPtr->dictionary = dictEnd - streamPtr->dictSize;
				}
			}

			var dictIssue =
				streamPtr->dictSize < 64 * KB && streamPtr->dictSize < streamPtr->currentOffset
					? dictIssue_directive.dictSmall
					: dictIssue_directive.noDictIssue;
			var dictMode =
				dictEnd == source
					? dict_directive.withPrefix64k
					: dict_directive.usingExtDict;

			var result = LZ4_compress_generic(
				streamPtr,
				source,
				dest,
				inputSize,
				maxOutputSize,
				limitedOutput_directive.limitedOutput,
				tableType_t.byU32,
				dictMode,
				dictIssue,
				(uint) acceleration);

			if (dictMode == dict_directive.withPrefix64k)
			{
				/* prefix mode : source data follows dictionary */
				streamPtr->dictSize += (uint) inputSize;
				streamPtr->currentOffset += (uint) inputSize;
			}
			else
			{
				/* external dictionary mode */
				streamPtr->dictionary = source;
				streamPtr->dictSize = (uint) inputSize;
				streamPtr->currentOffset += (uint) inputSize;
			}

			return result;
		}
	}
}
