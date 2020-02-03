//---------------------------------------------------------
//
// This file has been generated. All changes will be lost.
//
//---------------------------------------------------------
#define BIT32

using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;

// ReSharper disable InconsistentNaming
// ReSharper disable AccessToStaticMemberViaDerivedType

#if BIT32
using Mem = K4os.Compression.LZ4.Internal.Mem32;
using ptrT = System.Int32;
using sizeT = System.Int32;
#else
using Mem = K4os.Compression.LZ4.Internal.Mem64;
using ptrT = System.Int64;
using sizeT = System.Int32;
#endif

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
		protected static void LZ4_putPosition(
			byte* p, void* tableBase, tableType_t tableType, byte* srcBase)
		{
			LZ4_putPositionOnHash(p, LZ4_hashPosition(p, tableType), tableBase, tableType, srcBase);
		}
		
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		protected static byte* LZ4_getPosition(
			byte* p, void* tableBase, tableType_t tableType, byte* srcBase) =>
			LZ4_getPositionOnHash(LZ4_hashPosition(p, tableType), tableBase, tableType, srcBase);
		
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		protected static void LZ4_prepareTable(
			LZ4_stream_t* cctx, int inputSize, tableType_t tableType) 
		{
			if (cctx->dirty)
			{
				Mem.Zero((byte*)cctx, sizeof(LZ4_stream_t));
				return;
			}

			if (cctx->tableType != tableType_t.clearedTable) {
				Debug.Assert(inputSize >= 0);
				if (cctx->tableType != tableType
					|| ((tableType == tableType_t.byU16) && cctx->currentOffset + (uint)inputSize >= 0xFFFFU)
					|| ((tableType == tableType_t.byU32) && cctx->currentOffset > 1 * GB)
					|| tableType == tableType_t.byPtr
					|| inputSize >= 4 * KB)
				{
					Mem.Zero((byte*)cctx->hashTable, LZ4_HASHTABLESIZE);
					cctx->currentOffset = 0;
					cctx->tableType = tableType_t.clearedTable;
				}
			}

			if (cctx->currentOffset != 0 && tableType == tableType_t.byU32) {
				cctx->currentOffset += 64 * KB;
			}

			cctx->dictCtx = null;
			cctx->dictionary = null;
			cctx->dictSize = 0;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		int LZ4_compress_generic(
                 LZ4_stream_t* cctx,
                 byte* source,
                 byte* dest,
                 int inputSize,
                 int *inputConsumed, /* only written when outputDirective == fillOutput */
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
        dictDirective == dict_directive.usingDictCtx ? dictCtx->dictionary : cctx->dictionary;
    uint dictSize =
        dictDirective == dict_directive.usingDictCtx ? dictCtx->dictSize : cctx->dictSize;
    uint dictDelta = (dictDirective == dict_directive.usingDictCtx) ? startIndex - dictCtx->currentOffset : 0;

    bool maybe_extMem = (dictDirective == dict_directive.usingExtDict) || (dictDirective == dict_directive.usingDictCtx);
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

    if (outputDirective == limitedOutput_directive.fillOutput && maxOutputSize < 1) { return 0; }
    if ((uint)inputSize > (uint)LZ4_MAX_INPUT_SIZE) { return 0; }
    if ((tableType == tableType_t.byU16) && (inputSize>=LZ4_64Klimit)) { return 0; }
    if (tableType==tableType_t.byPtr) Debug.Assert(dictDirective==dict_directive.noDict);
    Debug.Assert(acceleration >= 1);

    lowLimit = (byte*)source - (dictDirective == dict_directive.withPrefix64k ? dictSize : 0);

    /* Update context state */
    if (dictDirective == dict_directive.usingDictCtx) {
        /* Subsequent linked blocks can't use the dictionary. */
        /* Instead, they use the block we just compressed. */
        cctx->dictCtx = null;
        cctx->dictSize = (uint)inputSize;
    } else {
        cctx->dictSize += (uint)inputSize;
    }
    cctx->currentOffset += (uint)inputSize;
    cctx->tableType = tableType;

    if (inputSize<LZ4_minLength) goto _last_literals;

    /* First Byte */
    LZ4_putPosition(ip, cctx->hashTable, tableType, @base);
    ip++; forwardH = LZ4_hashPosition(ip, tableType);

    /* Main Loop */
    for ( ; ; ) {
        byte* match;
        byte* token;
        byte* filledIp;

        /* Find a match */
        if (tableType == tableType_t.byPtr) {
            byte* forwardIp = ip;
            int step = 1;
            int searchMatchNb = acceleration << LZ4_skipTrigger;
            do {
                uint h = forwardH;
                ip = forwardIp;
                forwardIp += step;
                step = (searchMatchNb++ >> LZ4_skipTrigger);

                if ((forwardIp > mflimitPlusOne)) goto _last_literals;
                Debug.Assert(ip < mflimitPlusOne);

                match = LZ4_getPositionOnHash(h, cctx->hashTable, tableType, @base);
                forwardH = LZ4_hashPosition(forwardIp, tableType);
                LZ4_putPositionOnHash(ip, h, cctx->hashTable, tableType, @base);

            } while ( (match+LZ4_DISTANCE_MAX < ip) || (Mem.Peek32(match) != Mem.Peek32(ip)) );

        } else {   /* byU32, byU16 */

            byte* forwardIp = ip;
            int step = 1;
            int searchMatchNb = acceleration << LZ4_skipTrigger;
            do {
                uint h = forwardH;
                uint current = (uint)(forwardIp - @base);
                uint matchIndex = LZ4_getIndexOnHash(h, cctx->hashTable, tableType);
                Debug.Assert(matchIndex <= current);
                Debug.Assert(forwardIp - @base < (ptrT)(2 * GB - 1));
                ip = forwardIp;
                forwardIp += step;
                step = (searchMatchNb++ >> LZ4_skipTrigger);

                if ((forwardIp > mflimitPlusOne)) goto _last_literals;
                Debug.Assert(ip < mflimitPlusOne);

                if (dictDirective == dict_directive.usingDictCtx) {
                    if (matchIndex < startIndex) {
                        Debug.Assert(tableType == tableType_t.byU32);
                        matchIndex = LZ4_getIndexOnHash(h, dictCtx->hashTable, tableType_t.byU32);
                        match = dictBase + matchIndex;
                        matchIndex += dictDelta;
                        lowLimit = dictionary;
                    } else {
                        match = @base + matchIndex;
                        lowLimit = (byte*)source;
                    }
                } else if (dictDirective==dict_directive.usingExtDict) {
                    if (matchIndex < startIndex) {
                        Debug.Assert(startIndex - matchIndex >= MINMATCH);
                        match = dictBase + matchIndex;
                        lowLimit = dictionary;
                    } else {
                        match = @base + matchIndex;
                        lowLimit = (byte*)source;
                    }
                } else {
                    match = @base + matchIndex;
                }
                forwardH = LZ4_hashPosition(forwardIp, tableType);
                LZ4_putIndexOnHash(current, h, cctx->hashTable, tableType);

                if ((dictIssue == dictIssue_directive.dictSmall) && (matchIndex < prefixIdxLimit)) { continue; }
                Debug.Assert(matchIndex < current);
                if ( ((tableType != tableType_t.byU16) || (LZ4_DISTANCE_MAX < LZ4_DISTANCE_ABSOLUTE_MAX))
                  && (matchIndex+LZ4_DISTANCE_MAX < current)) {
                    continue;
                }
                Debug.Assert((current - matchIndex) <= LZ4_DISTANCE_MAX);

                if (Mem.Peek32(match) == Mem.Peek32(ip)) {
                    if (maybe_extMem) offset = current - matchIndex;
                    break;
                }

            } while(true);
        }

        /* Catch up */
        filledIp = ip;
        while (((ip>anchor) & (match > lowLimit)) && ((ip[-1]==match[-1]))) { ip--; match--; }

        /* Encode Literals */
        {   var litLength = (uint)(ip - anchor);
            token = op++;
            if ((outputDirective == limitedOutput_directive.limitedOutput) &&
                ((op + litLength + (2 + 1 + LASTLITERALS) + (litLength/255) > olimit)) ) {
                return 0;
            }
            if ((outputDirective == limitedOutput_directive.fillOutput) && ((op + (litLength+240)/255 + litLength + 2 + 1 + MFLIMIT - MINMATCH > olimit))) {
                op--;
                goto _last_literals;
            }
            if (litLength >= RUN_MASK) {
                int len = (int)(litLength - RUN_MASK);
                *token = (byte)(RUN_MASK<<ML_BITS);
                for(; len >= 255 ; len-=255) *op++ = 255;
                *op++ = (byte)len;
            }
            else *token = (byte)(litLength<<ML_BITS);

            /* Copy Literals */
            Mem.WildCopy8(op, anchor, op+litLength);
            op+=litLength;
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
            (op + 2 + 1 + MFLIMIT - MINMATCH > olimit)) {
            /* the match was too close to the end, rewind and go to last literals */
            op = token;
            goto _last_literals;
        }

        /* Encode Offset */
        if (maybe_extMem) {   /* static test */
            Debug.Assert(offset <= LZ4_DISTANCE_MAX && offset > 0);
            Mem.Poke16(op, (ushort)offset); op+=2;
        } else  {
            Debug.Assert(ip-match <= LZ4_DISTANCE_MAX);
            Mem.Poke16(op, (ushort)(ip - match)); op+=2;
        }

        /* Encode MatchLength */
        {   uint matchCode;

            if ( (dictDirective==dict_directive.usingExtDict || dictDirective==dict_directive.usingDictCtx)
              && (lowLimit==dictionary) /* match within extDict */ ) {
                byte* limit = ip + (dictEnd-match);
                Debug.Assert(dictEnd > match);
                if (limit > matchlimit) limit = matchlimit;
                matchCode = LZ4_count(ip+MINMATCH, match+MINMATCH, limit);
                ip += (uint)matchCode + MINMATCH;
                if (ip==limit) {
                    uint more = LZ4_count(limit, (byte*)source, matchlimit);
                    matchCode += more;
                    ip += more;
                }
            } else {
                matchCode = LZ4_count(ip+MINMATCH, match+MINMATCH, matchlimit);
                ip += (uint)matchCode + MINMATCH;
            }

            if ((outputDirective != 0) && ((op + (1 + LASTLITERALS) + (matchCode+240)/255 > olimit)) ) {
                if (outputDirective == limitedOutput_directive.fillOutput) {
                    /* Match description too long : reduce it */
                    uint newMatchCode = 15 - 1 + ((uint)(olimit - op) - 1 - LASTLITERALS) * 255;
                    ip -= matchCode - newMatchCode;
                    Debug.Assert(newMatchCode < matchCode);
                    matchCode = newMatchCode;
                    if ((ip <= filledIp)) {
                        /* We have already filled up to filledIp so if ip ends up less than filledIp
                         * we have positions in the hash table beyond the current position. This is
                         * a problem if we reuse the hash table. So we have to remove these positions
                         * from the hash table.
                         */
                        byte* ptr;
                        for (ptr = ip; ptr <= filledIp; ++ptr) {
                            uint h = LZ4_hashPosition(ptr, tableType);
                            LZ4_clearHash(h, cctx->hashTable, tableType);
                        }
                    }
                } else {
                    Debug.Assert(outputDirective == limitedOutput_directive.limitedOutput);
                    return 0;
                }
            }
            if (matchCode >= ML_MASK) {
                *token += (byte)ML_MASK; //!!!
                matchCode -= ML_MASK;
                Mem.Poke32(op, 0xFFFFFFFF);
                while (matchCode >= 4*255) {
                    op+=4;
                    Mem.Poke32(op, 0xFFFFFFFF);
                    matchCode -= 4*255;
                }
                op += matchCode / 255;
                *op++ = (byte)(matchCode % 255);
            } else
                *token += (byte)(matchCode);
        }
        /* Ensure we have enough space for the last literals. */
        Debug.Assert(!(outputDirective == limitedOutput_directive.fillOutput && op + 1 + LASTLITERALS > olimit));

        anchor = ip;

        /* Test end of chunk */
        if (ip >= mflimitPlusOne) break;

        /* Fill table */
        LZ4_putPosition(ip-2, cctx->hashTable, tableType, @base);

        /* Test next position */
        if (tableType == tableType_t.byPtr) {

            match = LZ4_getPosition(ip, cctx->hashTable, tableType, @base);
            LZ4_putPosition(ip, cctx->hashTable, tableType, @base);
            if ( (match+LZ4_DISTANCE_MAX >= ip) && (Mem.Peek32(match) == Mem.Peek32(ip)) )
            { token=op++; *token=0; goto _next_match; }

        } else {   /* byU32, byU16 */

            uint h = LZ4_hashPosition(ip, tableType);
            uint current = (uint)(ip-@base);
            uint matchIndex = LZ4_getIndexOnHash(h, cctx->hashTable, tableType);
            Debug.Assert(matchIndex < current);
            if (dictDirective == dict_directive.usingDictCtx) {
                if (matchIndex < startIndex) {
                    matchIndex = LZ4_getIndexOnHash(h, dictCtx->hashTable, tableType_t.byU32);
                    match = dictBase + matchIndex;
                    lowLimit = dictionary;
                    matchIndex += dictDelta;
                } else {
                    match = @base + matchIndex;
                    lowLimit = (byte*)source;
                }
            } else if (dictDirective==dict_directive.usingExtDict) {
                if (matchIndex < startIndex) {
                    match = dictBase + matchIndex;
                    lowLimit = dictionary;
                } else {
                    match = @base + matchIndex;
                    lowLimit = (byte*)source;
                }
            } else {
                match = @base + matchIndex;
            }
            LZ4_putIndexOnHash(current, h, cctx->hashTable, tableType);
            Debug.Assert(matchIndex < current);
            if ( ((dictIssue==dictIssue_directive.dictSmall) ? (matchIndex >= prefixIdxLimit) : true)
              && (((tableType==tableType_t.byU16) && (LZ4_DISTANCE_MAX == LZ4_DISTANCE_ABSOLUTE_MAX)) ? true : (matchIndex+LZ4_DISTANCE_MAX >= current))
              && (Mem.Peek32(match) == Mem.Peek32(ip)) ) {
                token=op++;
                *token=0;
                if (maybe_extMem) offset = current - matchIndex;
                goto _next_match;
            }
        }

        forwardH = LZ4_hashPosition(++ip, tableType);

    }

_last_literals:
    /* Encode Last Literals */
    {   var lastRun = (sizeT)(iend - anchor);
        if ( (outputDirective != 0) &&  /* Check output buffer overflow */
            (op + lastRun + 1 + ((lastRun+255-RUN_MASK)/255) > olimit)) {
            if (outputDirective == limitedOutput_directive.fillOutput) {
                Debug.Assert(olimit >= op);
                lastRun  = (sizeT)(olimit-op) - 1;
                lastRun -= (lastRun+240)/255;
            } else {
                Debug.Assert(outputDirective == limitedOutput_directive.limitedOutput);
                return 0;
            }
        }
        if (lastRun >= RUN_MASK) {
            var accumulator = (sizeT)(lastRun - RUN_MASK);
            *op++ = (byte)(RUN_MASK << ML_BITS);
            for(; accumulator >= 255 ; accumulator-=255) *op++ = 255;
            *op++ = (byte) accumulator;
        } else {
            *op++ = (byte)(lastRun<<ML_BITS);
        }
        Mem.Copy(op, anchor, lastRun);
        ip = anchor + lastRun;
        op += lastRun;
    }

    if (outputDirective == limitedOutput_directive.fillOutput) {
        *inputConsumed = (int) (((byte*)ip)-source);
    }
    result = (int)(((byte*)op) - dest);
    Debug.Assert(result > 0);
    return result;
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

