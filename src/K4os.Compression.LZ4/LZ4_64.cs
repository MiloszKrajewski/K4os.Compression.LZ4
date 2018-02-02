using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

// ReSharper disable InconsistentNaming

namespace K4os.Compression.LZ4
{
	internal unsafe class LZ4_64: LZ4_xx
	{
#if BIT32
		private const int STEPSIZE = 4;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static uint LZ4_read_ARCH(void* p) => *(uint*) p;

		private static readonly uint[] DeBruijnBytePos = {
			0, 0, 3, 0, 3, 1, 3, 0,
			3, 2, 2, 1, 3, 2, 0, 1,
			3, 3, 1, 2, 2, 2, 2, 0,
			3, 1, 2, 0, 1, 0, 1, 1
		};

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static uint LZ4_NbCommonBytes(uint val) =>
			DeBruijnBytePos[(uint) ((int) val & -(int) val) * 0x077CB531U >> 27];
#else
		private const int STEPSIZE = 8;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static ulong LZ4_read_ARCH(void* p) => *(ulong*) p;

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
		private static uint LZ4_NbCommonBytes(ulong val) =>
			DeBruijnBytePos[(ulong) ((long) val & -(long) val) * 0x0218A392CDABBD3Ful >> 58];
#endif

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static uint LZ4_count(byte* pIn, byte* pMatch, byte* pInLimit)
		{
			var pStart = pIn;

			if (pIn < pInLimit - (STEPSIZE - 1))
			{
				var diff = LZ4_read_ARCH(pMatch) ^ LZ4_read_ARCH(pIn);
				if (diff != 0)
				{
					pIn += STEPSIZE;
					pMatch += STEPSIZE;
				}
				else
				{
					return LZ4_NbCommonBytes(diff);
				}
			}

			while (pIn < pInLimit - (STEPSIZE - 1))
			{
				var diff = LZ4_read_ARCH(pMatch) ^ LZ4_read_ARCH(pIn);
				if (diff != 0)
				{
					pIn += STEPSIZE;
					pMatch += STEPSIZE;
					continue;
				}

				pIn += LZ4_NbCommonBytes(diff);
				return (uint) (pIn - pStart);
			}

#if !BIT32
			if (pIn < pInLimit - 3 && LZ4_read32(pMatch) == LZ4_read32(pIn))
			{
				pIn += 4;
				pMatch += 4;
			}
#endif
			if (pIn < pInLimit - 1 && LZ4_read16(pMatch) == LZ4_read16(pIn))
			{
				pIn += 2;
				pMatch += 2;
			}

			if (pIn < pInLimit && *pMatch == *pIn) pIn++;
			return (uint) (pIn - pStart);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		static uint LZ4_hashPosition(void* p, tableType_t tableType)
		{
#if !BIT32
			if (tableType != tableType_t.byU16)
				return LZ4_hash5(LZ4_read_ARCH(p), tableType);
#endif
			return LZ4_hash4(LZ4_read32(p), tableType);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		static void LZ4_putPositionOnHash(
			byte* p, uint h, void* tableBase, tableType_t tableType, byte* srcBase)
		{
			switch (tableType)
			{
				case tableType_t.byPtr:
					((byte**) tableBase)[h] = p;
					return;
				case tableType_t.byU32:
					((uint*) tableBase)[h] = (uint) (p - srcBase);
					return;
				case tableType_t.byU16:
					((ushort*) tableBase)[h] = (ushort) (p - srcBase);
					return;
			}
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		static void LZ4_putPosition(byte* p, void* tableBase, tableType_t tableType, byte* srcBase) =>
			LZ4_putPositionOnHash(p, LZ4_hashPosition(p, tableType), tableBase, tableType, srcBase);

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		static byte* LZ4_getPositionOnHash(uint h, void* tableBase, tableType_t tableType, byte* srcBase)
		{
			switch (tableType)
			{
				case tableType_t.byPtr: return ((byte**) tableBase)[h];
				case tableType_t.byU32: return ((uint*) tableBase)[h] + srcBase;
				default: return ((ushort*) tableBase)[h] + srcBase;
			}
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		static byte* LZ4_getPosition(byte* p, void* tableBase, tableType_t tableType, byte* srcBase) =>
			LZ4_getPositionOnHash(LZ4_hashPosition(p, tableType), tableBase, tableType, srcBase);

		static int LZ4_compress_generic(
			LZ4_stream_t_internal* cctx,
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
			byte* base_;
			byte* lowLimit;
			var lowRefLimit = ip - cctx->dictSize;
			var dictionary = cctx->dictionary;
			var dictEnd = dictionary + cctx->dictSize;
			var dictDelta = (int) (dictEnd - source);
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
					base_ = source - cctx->currentOffset;
					lowLimit = source - cctx->dictSize;
					break;
				case dict_directive.usingExtDict:
					base_ = source - cctx->currentOffset;
					lowLimit = source;
					break;
				default:
					base_ = source;
					lowLimit = source;
					break;
			}

			if (tableType == tableType_t.byU16 && inputSize >= LZ4_64Klimit)
				return 0;

			if (inputSize < LZ4_minLength)
				goto _last_literals;

			LZ4_putPosition(ip, cctx->hashTable, tableType, base_);
			ip++;
			var forwardH = LZ4_hashPosition(ip, tableType);

			for (;;)
			{
				var refDelta = 0;
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
						step = (searchMatchNb++ >> LZ4_skipTrigger);

						if (forwardIp > mflimit)
							goto _last_literals;

						match = LZ4_getPositionOnHash(h, cctx->hashTable, tableType, base_);
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
						LZ4_putPositionOnHash(ip, h, cctx->hashTable, tableType, base_);
					}
					while (
						dictIssue == dictIssue_directive.dictSmall && match < lowRefLimit
						|| tableType != tableType_t.byU16 && match + MAX_DISTANCE < ip
						|| LZ4_read32(match + refDelta) != LZ4_read32(ip)
					);
				}

				/* Catch up */
				while (ip > anchor && match + refDelta > lowLimit && ip[-1] == match[refDelta - 1])
				{
					ip--;
					match--;
				}

				/* Encode Literals */
				{
					var litLength = (uint) (ip - anchor);
					token = op++;
					if (outputLimited != limitedOutput_directive.notLimited
						&& op + litLength + (2 + 1 + LASTLITERALS) + litLength / 255 > olimit)
						return 0;

					if (litLength >= RUN_MASK)
					{
						var len = (int) litLength - RUN_MASK;

						*token = (byte) (RUN_MASK << ML_BITS);
						for (; len >= 255; len -= 255) *op++ = 255;
						*op++ = (byte) len;
					}
					else
					{
						*token = (byte) (litLength << ML_BITS);
					}

					LZ4_wildCopy((ulong*) op, (ulong*) anchor, op + litLength);
					op += litLength;
				}

				_next_match:

				LZ4_write16(op, (ushort) (ip - match));
				op += 2;

				/* Encode MatchLength */
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

					if (outputLimited != limitedOutput_directive.notLimited
						&& op + (1 + LASTLITERALS) + (matchCode >> 8) > olimit)
						return 0;

					if (matchCode >= ML_MASK)
					{
						*token += (byte) ML_MASK;
						matchCode -= ML_MASK;
						LZ4_write32(op, 0xFFFFFFFF);
						while (matchCode >= 4 * 255)
						{
							op += 4;
							LZ4_write32(op, 0xFFFFFFFF);
							matchCode -= 4 * 255;
						}

						op += matchCode / 255;
						*op++ = (byte) (matchCode % 255);
					}
					else
					{
						*token += (byte) (matchCode);
					}
				}

				anchor = ip;

				if (ip > mflimit) break;

				LZ4_putPosition(ip - 2, cctx->hashTable, tableType, base_);

				match = LZ4_getPosition(ip, cctx->hashTable, tableType, base_);
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

				LZ4_putPosition(ip, cctx->hashTable, tableType, base_);
				if ((dictIssue != dictIssue_directive.dictSmall || match >= lowRefLimit)
					&& match + MAX_DISTANCE >= ip
					&& LZ4_read32(match + refDelta) == LZ4_read32(ip))
				{
					token = op++;
					*token = 0;
					goto _next_match;
				}

				/* Prepare next loop */
				forwardH = LZ4_hashPosition(++ip, tableType);
			}

			_last_literals:
			{
				var lastRun = (int) (iend - anchor);
				if (outputLimited != limitedOutput_directive.notLimited
					&& op - dest + lastRun + 1 + (lastRun + 255 - RUN_MASK) / 255 > (uint) maxOutputSize)
					return 0;

				if
					(lastRun >= RUN_MASK)
				{
					var accumulator = lastRun - RUN_MASK;
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

		static int LZ4_compress_fast_extState(
			void* state, byte* source, byte* dest, int inputSize, int maxOutputSize, int acceleration)
		{
			var ctx = &((LZ4_stream_t*) state)->internal_donotuse;
			LZ4_resetStream((LZ4_stream_t*) state);
			if (acceleration < 1) acceleration = ACCELERATION_DEFAULT;

			var limited =
				maxOutputSize >= LZ4_COMPRESSBOUND(inputSize)
					? limitedOutput_directive.notLimited
					: limitedOutput_directive.limitedOutput;
			var tableType =
				inputSize < LZ4_64Klimit ? tableType_t.byU16 :
				IntPtr.Size == 8 ? tableType_t.byU32 :
				tableType_t.byPtr;

			return LZ4_compress_generic(
				ctx,
				source,
				dest,
				inputSize,
				limited == limitedOutput_directive.notLimited ? 0 : maxOutputSize,
				limited,
				tableType,
				dict_directive.noDict,
				dictIssue_directive.noDictIssue,
				(uint) acceleration);
		}

		private static void LZ4_resetStream(LZ4_stream_t* state) =>
			Mem.Zero((byte*) state, sizeof(LZ4_stream_t));

		static int LZ4_compress_fast(
			byte* source, byte* dest, int inputSize, int maxOutputSize, int acceleration)
		{
			LZ4_stream_t ctx;
			return LZ4_compress_fast_extState(&ctx, source, dest, inputSize, maxOutputSize, acceleration);
		}

		internal static int LZ4_compress_default(byte* source, byte* dest, int inputSize, int maxOutputSize) =>
			LZ4_compress_fast(source, dest, inputSize, maxOutputSize, 1);

		static int LZ4_compress_destSize_generic(
			LZ4_stream_t_internal* ctx, byte* src, byte* dst, int* srcSizePtr, int targetDstSize,
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

			/* Main Loop */
			for (;;)
			{
				byte* match;
				byte* token;

				/* Find a match */
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

						if (forwardIp > mflimit)
							goto _last_literals;

						match = LZ4_getPositionOnHash(h, ctx->hashTable, tableType, base_);
						forwardH = LZ4_hashPosition(forwardIp, tableType);
						LZ4_putPositionOnHash(ip, h, ctx->hashTable, tableType, base_);
					}
					while (
						tableType != tableType_t.byU16 && match + MAX_DISTANCE < ip
						|| LZ4_read32(match) != LZ4_read32(ip));
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

					LZ4_wildCopy((ulong*) op, (ulong*) anchor, op + litLength);
					op += litLength;
				}

				_next_match:
				LZ4_write16(op, (ushort) (ip - match));
				op += 2;

				{
					var matchLength = (int) LZ4_count(ip + MINMATCH, match + MINMATCH, matchlimit);

					if (op + ((matchLength + 240) / 255) > oMaxMatch)
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
				if (match + MAX_DISTANCE >= ip && LZ4_read32(match) == LZ4_read32(ip))
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

		static int LZ4_compress_destSize_extState(
			LZ4_stream_t* state, byte* src, byte* dst, int* srcSizePtr, int targetDstSize)
		{
			LZ4_resetStream(state);

			if (targetDstSize >= LZ4_COMPRESSBOUND(*srcSizePtr))
			{
				return LZ4_compress_fast_extState(state, src, dst, *srcSizePtr, targetDstSize, 1);
			}

			var tableType =
				*srcSizePtr < LZ4_64Klimit ? tableType_t.byU16 :
				sizeof(void*) == 8 ? tableType_t.byU32 :
				tableType_t.byPtr;

			return LZ4_compress_destSize_generic(
				&state->internal_donotuse,
				src,
				dst,
				srcSizePtr,
				targetDstSize,
				tableType);
		}

		static int LZ4_compress_destSize(byte* src, byte* dst, int* srcSizePtr, int targetDstSize)
		{
			LZ4_stream_t ctxBody;
			return LZ4_compress_destSize_extState(&ctxBody, src, dst, srcSizePtr, targetDstSize);
		}
	}
}
