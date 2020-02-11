// ReSharper disable IdentifierTypo
// ReSharper disable InconsistentNaming
// ReSharper disable AccessToStaticMemberViaDerivedType
// ReSharper disable ConditionIsAlwaysTrueOrFalse
// ReSharper disable BuiltInTypeReferenceStyle

using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace K4os.Compression.LZ4.Engine
{
	#if BIT32
	using Mem = Internal.Mem32;
	using ptr_t = Int32;
	using size_t = Int32;
	#else
	using Mem = Internal.Mem64;
	using ptr_t = Int64;
	using size_t = Int32;

	#endif

	#if BIT32
	internal unsafe class LLDec32: LLTools32
	#else
	internal unsafe class LLDec64: LLTools64
	#endif
	{
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
public static int LZ4_decompress_generic(
                 byte* src,
                 byte* dst,
                 int srcSize,
                 int outputSize,

                 endCondition_directive endOnInput,
                 earlyEnd_directive partialDecoding,
                 dict_directive dict,
                 byte* lowPrefix,
                 byte* dictStart,
                 size_t dictSize
                 )
{
    if (src == null) { return -1; }

    {   byte* ip = (byte*) src;
        byte* iend = ip + srcSize;

        byte* op = (byte*) dst;
        byte* oend = op + outputSize;
        byte* cpy;

        byte* dictEnd = (dictStart == null) ? null : dictStart + dictSize;

        var isEndOnInput = endOnInput == endCondition_directive.endOnInputSize;
        var partialDecoding

        bool safeDecode = isEndOnInput;
        bool checkOffset = ((safeDecode) && (dictSize < (int)(64 * KB)));


        /* Set up the "end" pointers for the shortcut. */
        byte* shortiend = iend - (isEndOnInput ? 14 : 8) /*maxLL*/ - 2 /*offset*/;
        byte* shortoend = oend - (isEndOnInput ? 14 : 8) /*maxLL*/ - 18 /*maxML*/;

        byte* match;
        size_t offset;
        uint token;
        size_t length;


        /* Special cases */
        Debug.Assert(lowPrefix <= op);
        if ((isEndOnInput) && ((outputSize==0))) {
            /* Empty output buffer */
            if (partialDecoding) return 0;
            return ((srcSize==1) && (*ip==0)) ? 0 : -1;
        }
        if ((!endOnInput) && ((outputSize==0))) { return (*ip==0 ? 1 : -1); }
        if ((endOnInput) && (srcSize==0)) { return -1; }

	/* Currently the fast loop shows a regression on qualcomm arm chips. */
#if LZ4_FAST_DEC_LOOP
        if ((oend - op) < FASTLOOP_SAFE_DISTANCE) {
            DEBUGLOG(6, "skip fast decode loop");
            goto safe_decode;
        }

        /* Fast loop : decode sequences as long as output < iend-FASTLOOP_SAFE_DISTANCE */
        while (1) {
            /* Main fastloop assertion: We can always wildcopy FASTLOOP_SAFE_DISTANCE */
            Debug.Assert(oend - op >= FASTLOOP_SAFE_DISTANCE);
            if (endOnInput) { Debug.Assert(ip < iend); }
            token = *ip++;
            length = token >> ML_BITS;  /* literal length */

            Debug.Assert(!endOnInput || ip <= iend); /* ip < iend before the increment */

            /* decode literal length */
            if (length == RUN_MASK) {
                variable_length_error error = ok;
                length += read_variable_length(&ip, iend-RUN_MASK, endOnInput, endOnInput, &error);
                if (error == initial_error) { goto _output_error; }
                if ((safeDecode) && ((ptr_t)(op)+length<(ptr_t)(op))) { goto _output_error; } /* overflow detection */
                if ((safeDecode) && ((ptr_t)(ip)+length<(ptr_t)(ip))) { goto _output_error; } /* overflow detection */

                /* copy literals */
                cpy = op+length;
                LZ4_STATIC_ASSERT(MFLIMIT >= WILDCOPYLENGTH);
                if (endOnInput) {  /* LZ4_decompress_safe() */
                    if ((cpy>oend-32) || (ip+length>iend-32)) { goto safe_literal_copy; }
                    LZ4_wildCopy32(op, ip, cpy);
                } else {   /* LZ4_decompress_fast() */
                    if (cpy>oend-8) { goto safe_literal_copy; }
                    Mem.WildCopy8(op, ip, cpy); /* LZ4_decompress_fast() cannot copy more than 8 bytes at a time :
                                                 * it doesn't know input length, and only relies on end-of-block properties */
                }
                ip += length; op = cpy;
            } else {
                cpy = op+length;
                if (endOnInput) {  /* LZ4_decompress_safe() */
                    DEBUGLOG(7, "copy %u bytes in a 16-bytes stripe", (unsigned)length);
                    /* We don't need to check oend, since we check it once for each loop below */
                    if (ip > iend-(16 + 1/*max lit + offset + nextToken*/)) { goto safe_literal_copy; }
                    /* Literals can only be 14, but hope compilers optimize if we copy by a register size */
                    memcpy(op, ip, 16);
                } else {  /* LZ4_decompress_fast() */
                    /* LZ4_decompress_fast() cannot copy more than 8 bytes at a time :
                     * it doesn't know input length, and relies on end-of-block properties */
                    memcpy(op, ip, 8);
                    if (length > 8) { memcpy(op+8, ip+8, 8); }
                }
                ip += length; op = cpy;
            }

            /* get offset */
            offset = LZ4_readLE16(ip); ip+=2;
            match = op - offset;
            Debug.Assert(match <= op);

            /* get matchlength */
            length = token & ML_MASK;

            if (length == ML_MASK) {
              variable_length_error error = ok;
              if ((checkOffset) && ((match + dictSize < lowPrefix))) { goto _output_error; } /* Error : offset outside buffers */
              length += read_variable_length(&ip, iend - LASTLITERALS + 1, endOnInput, 0, &error);
              if (error != ok) { goto _output_error; }
                if ((safeDecode) && ((ptr_t)(op)+length<(ptr_t)op)) { goto _output_error; } /* overflow detection */
                length += MINMATCH;
                if (op + length >= oend - FASTLOOP_SAFE_DISTANCE) {
                    goto safe_match_copy;
                }
            } else {
                length += MINMATCH;
                if (op + length >= oend - FASTLOOP_SAFE_DISTANCE) {
                    goto safe_match_copy;
                }

                /* Fastpath check: Avoids a branch in LZ4_wildCopy32 if true */
                if ((dict == withPrefix64k) || (match >= lowPrefix)) {
                    if (offset >= 8) {
                        Debug.Assert(match >= lowPrefix);
                        Debug.Assert(match <= op);
                        Debug.Assert(op + 18 <= oend);

                        memcpy(op, match, 8);
                        memcpy(op+8, match+8, 8);
                        memcpy(op+16, match+16, 2);
                        op += length;
                        continue;
            }   }   }

            if ((checkOffset) && ((match + dictSize < lowPrefix))) { goto _output_error; } /* Error : offset outside buffers */
            /* match starting within external dictionary */
            if ((dict==usingExtDict) && (match < lowPrefix)) {
                if ((op+length > oend-LASTLITERALS)) {
                    if (partialDecoding) {
                        length = MIN(length, (size_t)(oend-op));  /* reach end of buffer */
                    } else {
                        goto _output_error;  /* end-of-block condition violated */
                }   }

                if (length <= (size_t)(lowPrefix-match)) {
                    /* match fits entirely within external dictionary : just copy */
                    memmove(op, dictEnd - (lowPrefix-match), length);
                    op += length;
                } else {
                    /* match stretches into both external dictionary and current block */
                    size_t copySize = (size_t)(lowPrefix - match);
                    size_t restSize = length - copySize;
                    memcpy(op, dictEnd - copySize, copySize);
                    op += copySize;
                    if (restSize > (size_t)(op - lowPrefix)) {  /* overlap copy */
                        byte* endOfMatch = op + restSize;
                        byte* copyFrom = lowPrefix;
                        while (op < endOfMatch) { *op++ = *copyFrom++; }
                    } else {
                        memcpy(op, lowPrefix, restSize);
                        op += restSize;
                }   }
                continue;
            }

            /* copy match within block */
            cpy = op + length;

            Debug.Assert((op <= oend) && (oend-op >= 32));
            if ((offset<16)) {
                LZ4_memcpy_using_offset(op, match, cpy, offset);
            } else {
                LZ4_wildCopy32(op, match, cpy);
            }

            op = cpy;   /* wildcopy correction */
        }
    safe_decode:
#endif

        /* Main Loop : decode remaining sequences where output < FASTLOOP_SAFE_DISTANCE */
        while (1) {
            token = *ip++;
            length = token >> ML_BITS;  /* literal length */

            Debug.Assert(!endOnInput || ip <= iend); /* ip < iend before the increment */

            /* A two-stage shortcut for the most common case:
             * 1) If the literal length is 0..14, and there is enough space,
             * enter the shortcut and copy 16 bytes on behalf of the literals
             * (in the fast mode, only 8 bytes can be safely copied this way).
             * 2) Further if the match length is 4..18, copy 18 bytes in a similar
             * manner; but we ensure that there's enough space in the output for
             * those 18 bytes earlier, upon entering the shortcut (in other words,
             * there is a combined check for both stages).
             */
            if ( (endOnInput ? length != RUN_MASK : length <= 8)
                /* strictly "less than" on input, to re-enter the loop with at least one byte */
              && ((endOnInput ? ip < shortiend : 1) & (op <= shortoend)) ) {
                /* Copy the literals */
                memcpy(op, ip, endOnInput ? 16 : 8);
                op += length; ip += length;

                /* The second stage: prepare for match copying, decode full info.
                 * If it doesn't work out, the info won't be wasted. */
                length = token & ML_MASK; /* match length */
                offset = LZ4_readLE16(ip); ip += 2;
                match = op - offset;
                Debug.Assert(match <= op); /* check overflow */

                /* Do not deal with overlapping matches. */
                if ( (length != ML_MASK)
                  && (offset >= 8)
                  && (dict==withPrefix64k || match >= lowPrefix) ) {
                    /* Copy the match. */
                    memcpy(op + 0, match + 0, 8);
                    memcpy(op + 8, match + 8, 8);
                    memcpy(op +16, match +16, 2);
                    op += length + MINMATCH;
                    /* Both stages worked, load the next token. */
                    continue;
                }

                /* The second stage didn't work out, but the info is ready.
                 * Propel it right to the point of match copying. */
                goto _copy_match;
            }

            /* decode literal length */
            if (length == RUN_MASK) {
                variable_length_error error = ok;
                length += read_variable_length(&ip, iend-RUN_MASK, endOnInput, endOnInput, &error);
                if (error == initial_error) { goto _output_error; }
                if ((safeDecode) && ((ptr_t)(op)+length<(ptr_t)(op))) { goto _output_error; } /* overflow detection */
                if ((safeDecode) && ((ptr_t)(ip)+length<(ptr_t)(ip))) { goto _output_error; } /* overflow detection */
            }

            /* copy literals */
            cpy = op+length;
#if LZ4_FAST_DEC_LOOP
        safe_literal_copy:
#endif
            LZ4_STATIC_ASSERT(MFLIMIT >= WILDCOPYLENGTH);
            if ( ((endOnInput) && ((cpy>oend-MFLIMIT) || (ip+length>iend-(2+1+LASTLITERALS))) )
              || ((!endOnInput) && (cpy>oend-WILDCOPYLENGTH)) )
            {
                /* We've either hit the input parsing restriction or the output parsing restriction.
                 * If we've hit the input parsing condition then this must be the last sequence.
                 * If we've hit the output parsing condition then we are either using partialDecoding
                 * or we've hit the output parsing condition.
                 */
                if (partialDecoding) {
                    /* Since we are partial decoding we may be in this block because of the output parsing
                     * restriction, which is not valid since the output buffer is allowed to be undersized.
                     */
                    Debug.Assert(endOnInput);
                    /* If we're in this block because of the input parsing condition, then we must be on the
                     * last sequence (or invalid), so we must check that we exactly consume the input.
                     */
                    if ((ip+length>iend-(2+1+LASTLITERALS)) && (ip+length != iend)) { goto _output_error; }
                    Debug.Assert(ip+length <= iend);
                    /* We are finishing in the middle of a literals segment.
                     * Break after the copy.
                     */
                    if (cpy > oend) {
                        cpy = oend;
                        Debug.Assert(op<=oend);
                        length = (size_t)(oend-op);
                    }
                    Debug.Assert(ip+length <= iend);
                } else {
                    /* We must be on the last sequence because of the parsing limitations so check
                     * that we exactly regenerate the original size (must be exact when !endOnInput).
                     */
                    if ((!endOnInput) && (cpy != oend)) { goto _output_error; }
                     /* We must be on the last sequence (or invalid) because of the parsing limitations
                      * so check that we exactly consume the input and don't overrun the output buffer.
                      */
                    if ((endOnInput) && ((ip+length != iend) || (cpy > oend))) { goto _output_error; }
                }
                memmove(op, ip, length);  /* supports overlapping memory regions, which only matters for in-place decompression scenarios */
                ip += length;
                op += length;
                /* Necessarily EOF when !partialDecoding. When partialDecoding
                 * it is EOF if we've either filled the output buffer or hit
                 * the input parsing restriction.
                 */
                if (!partialDecoding || (cpy == oend) || (ip == iend)) {
                    break;
                }
            } else {
                Mem.WildCopy8(op, ip, cpy);   /* may overwrite up to WILDCOPYLENGTH beyond cpy */
                ip += length; op = cpy;
            }

            /* get offset */
            offset = LZ4_readLE16(ip); ip+=2;
            match = op - offset;

            /* get matchlength */
            length = token & ML_MASK;

    _copy_match:
            if (length == ML_MASK) {
              variable_length_error error = ok;
              length += read_variable_length(&ip, iend - LASTLITERALS + 1, endOnInput, 0, &error);
              if (error != ok) goto _output_error;
                if ((safeDecode) && ((ptr_t)(op)+length<(ptr_t)op)) goto _output_error;   /* overflow detection */
            }
            length += MINMATCH;

#if LZ4_FAST_DEC_LOOP
        safe_match_copy:
#endif
            if ((checkOffset) && ((match + dictSize < lowPrefix))) goto _output_error;   /* Error : offset outside buffers */
            /* match starting within external dictionary */
            if ((dict==usingExtDict) && (match < lowPrefix)) {
                if ((op+length > oend-LASTLITERALS)) {
                    if (partialDecoding) length = MIN(length, (size_t)(oend-op));
                    else goto _output_error;   /* doesn't respect parsing restriction */
                }

                if (length <= (size_t)(lowPrefix-match)) {
                    /* match fits entirely within external dictionary : just copy */
                    memmove(op, dictEnd - (lowPrefix-match), length);
                    op += length;
                } else {
                    /* match stretches into both external dictionary and current block */
                    size_t copySize = (size_t)(lowPrefix - match);
                    size_t restSize = length - copySize;
                    memcpy(op, dictEnd - copySize, copySize);
                    op += copySize;
                    if (restSize > (size_t)(op - lowPrefix)) {  /* overlap copy */
                        byte* endOfMatch = op + restSize;
                        byte* copyFrom = lowPrefix;
                        while (op < endOfMatch) *op++ = *copyFrom++;
                    } else {
                        memcpy(op, lowPrefix, restSize);
                        op += restSize;
                }   }
                continue;
            }
            Debug.Assert(match >= lowPrefix);

            /* copy match within block */
            cpy = op + length;

            /* partialDecoding : may end anywhere within the block */
            Debug.Assert(op<=oend);
            if (partialDecoding && (cpy > oend-MATCH_SAFEGUARD_DISTANCE)) {
                size_t mlen = MIN(length, (size_t)(oend-op));
                byte* matchEnd = match + mlen;
                byte* copyEnd = op + mlen;
                if (matchEnd > op) {   /* overlap copy */
                    while (op < copyEnd) { *op++ = *match++; }
                } else {
                    memcpy(op, match, mlen);
                }
                op = copyEnd;
                if (op == oend) { break; }
                continue;
            }

            if ((offset<8)) {
                Mem.Poke4(op, 0);   /* silence msan warning when offset==0 */
                op[0] = match[0];
                op[1] = match[1];
                op[2] = match[2];
                op[3] = match[3];
                match += inc32table[offset];
                memcpy(op+4, match, 4);
                match -= dec64table[offset];
            } else {
                memcpy(op, match, 8);
                match += 8;
            }
            op += 8;

            if ((cpy > oend-MATCH_SAFEGUARD_DISTANCE)) {
                byte* oCopyLimit = oend - (WILDCOPYLENGTH-1);
                if (cpy > oend-LASTLITERALS) { goto _output_error; } /* Error : last LASTLITERALS bytes must be literals (uncompressed) */
                if (op < oCopyLimit) {
                    Mem.WildCopy8(op, match, oCopyLimit);
                    match += oCopyLimit - op;
                    op = oCopyLimit;
                }
                while (op < cpy) { *op++ = *match++; }
            } else {
                memcpy(op, match, 8);
                if (length > 16)  { Mem.WildCopy8(op+8, match+8, cpy); }
            }
            op = cpy;   /* wildcopy correction */
        }

        /* end of decoding */
        if (endOnInput) {
           return (int) (((byte*)op)-dst);     /* Nb of output bytes decoded */
       } else {
           return (int) (((byte*)ip)-src);   /* Nb of input bytes read */
       }

        /* Overflow error detected */
    _output_error:
        return (int) (-(((byte*)ip)-src))-1;
    }
}

	}
}