using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using uint16 = System.UInt16;
using uint32 = System.UInt32;

namespace K4os.Compression.LZ4
{
#if BIT32
	using size_t = System.UInt32;
	using reg_t = System.UInt32;

	internal unsafe class LZ4_64_HC: LZ4_32
	{
#else
	using size_t = System.UInt64;
	using reg_t = System.UInt64;

	internal unsafe class LZ4_64_HC: LZ4_64
	{
#endif
		const int LZ4HC_CLEVEL_MIN = 3;
		const int LZ4HC_CLEVEL_DEFAULT = 9;
		const int LZ4HC_CLEVEL_OPT_MIN = 10;
		const int LZ4HC_CLEVEL_MAX = 12;

		const int LZ4HC_DICTIONARY_LOGSIZE = 16;
		const int LZ4HC_MAXD = 1 << LZ4HC_DICTIONARY_LOGSIZE;
		const int LZ4HC_MAXD_MASK = LZ4HC_MAXD - 1;

		const int LZ4HC_HASH_LOG = 15;
		const int LZ4HC_HASHTABLESIZE = (1 << LZ4HC_HASH_LOG);
		const int LZ4HC_HASH_MASK = (LZ4HC_HASHTABLESIZE - 1);

		[StructLayout(LayoutKind.Sequential)]
		struct LZ4HC_CCtx_t
		{
			public fixed uint hashTable[LZ4HC_HASHTABLESIZE];
			public fixed ushort chainTable[LZ4HC_MAXD];
			public byte* end; /* next block here to continue on current prefix */
			public byte* basep; /* All index relative to this position */
			public byte* dictBase; /* alternate base for extDict */
			public byte* inputBuffer; /* deprecated */
			public uint dictLimit; /* below that point, need extDict */
			public uint lowLimit; /* below that point, no more dict */
			public uint nextToUpdate; /* index from which to continue dictionary update */
			public int compressionLevel;
		}
		//LZ4_streamHC_u

		enum repeat_state_e
		{
			rep_untested,
			rep_not,
			rep_confirmed
		};

		//const int LZ4_STREAMHCSIZE = (4*LZ4HC_HASHTABLESIZE + 2*LZ4HC_MAXD + 56);
		//const int LZ4_STREAMHCSIZE_SIZET = (LZ4_STREAMHCSIZE / sizeof(size_t));

		//union LZ4_streamHC_u
		//{
		//	size_t table [LZ4_STREAMHCSIZE_SIZET];
		//	LZ4HC_CCtx_internal internal_donotuse;
		//};   /* previously typedef'd to LZ4_streamHC_t */

		const int OPTIMAL_ML = (int) (ML_MASK - 1 + MINMATCH);

#warning uint? it was macro so types were not specified
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static uint HASH_FUNCTION(uint i) => (i * 2654435761U) >> (MINMATCH * 8 - LZ4HC_HASH_LOG);

#warning ulong*? it was macro so types were not specified
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static ulong DELTANEXTMAXD(ulong* chainTable, int p) => chainTable[p & LZ4HC_MAXD_MASK];

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static ushort DELTANEXTU16(ushort* table, ushort pos) => table[pos];

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static void DELTANEXTU16(ushort* table, ushort pos, ushort value) => table[pos] = value;

		private static uint LZ4HC_hashPtr(void* ptr) => HASH_FUNCTION(LZ4_read32(ptr));

		private static void LZ4HC_init(LZ4HC_CCtx_t* hc4, byte* start)
		{
			Mem.Zero((byte*) hc4->hashTable, LZ4HC_HASHTABLESIZE * sizeof(uint));
			Mem.Fill((byte*) hc4->chainTable, 0xFF, LZ4HC_MAXD * sizeof(ushort));
			hc4->nextToUpdate = 64 * KB;
			hc4->basep = start - 64 * KB;
			hc4->end = start;
			hc4->dictBase = start - 64 * KB;
			hc4->dictLimit = 64 * KB;
			hc4->lowLimit = 64 * KB;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static void LZ4HC_insert(LZ4HC_CCtx_t* hc4, byte* ip)
		{
			var chainTable = hc4->chainTable;
			var hashTable = hc4->hashTable;
			var basep = hc4->basep;
			var target = (uint) (ip - basep);
			var idx = hc4->nextToUpdate;

			while (idx < target)
			{
				var h = LZ4HC_hashPtr(basep + idx);
				var delta = idx - hashTable[h];
				if (delta > MAX_DISTANCE) delta = MAX_DISTANCE;
				DELTANEXTU16(chainTable, (ushort) idx, (ushort) delta);
				hashTable[h] = idx;
				idx++;
			}

			hc4->nextToUpdate = target;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		static int LZ4HC_countBack(byte* ip, byte* match, byte* iMin, byte* mMin)
		{
			var back = 0;
			while (ip + back > iMin && match + back > mMin && ip[back - 1] == match[back - 1]) back--;
			return back;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		static uint LZ4HC_countPattern(byte* ip, byte* iEnd, uint pattern32)
		{
			var iStart = ip;
#if BIT32
			var pattern = pattern32;
#else
			var pattern = pattern32 | ((ulong) pattern32 << 32);
#endif

			while (ip < iEnd - (STEPSIZE - 1))
			{
				var diff = LZ4_read_ARCH(ip) ^ pattern;
				if (diff != 0)
				{
					ip += LZ4_NbCommonBytes(diff);
					return (uint) (ip - iStart);
				}

				ip += STEPSIZE;
			}

			var patternByte = pattern;
			while (ip < iEnd && *ip == (byte) patternByte)
			{
				ip++;
				patternByte >>= 8;
			}

			return (uint) (ip - iStart);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		static uint LZ4HC_reverseCountPattern(byte* ip, byte* iLow, uint pattern)
		{
			var iStart = ip;

			while (ip >= iLow + 4)
			{
				if (LZ4_read32(ip - 4) != pattern) break;

				ip -= 4;
			}

			{
				var bytePtr = (byte*) &pattern + 3; /* works for any endianess */
				while (ip > iLow)
				{
					if (ip[-1] != *bytePtr) break;

					ip--;
					bytePtr--;
				}
			}

			return (uint) (iStart - ip);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		static int LZ4HC_InsertAndGetWiderMatch(
			LZ4HC_CCtx_t* hc4,
			byte* ip,
			byte* iLowLimit,
			byte* iHighLimit,
			int longest,
			byte** matchpos,
			byte** startpos,
			int maxNbAttempts,
			int patternAnalysis)
		{
			var chainTable = hc4->chainTable;
			var hashTable = hc4->hashTable;
			var basep = hc4->basep;
			var dictLimit = hc4->dictLimit;
			var lowPrefixPtr = basep + dictLimit;
			var lowLimit =
				hc4->lowLimit + 64 * KB > (uint) (ip - basep)
					? hc4->lowLimit
					: (uint) (ip - basep) - MAX_DISTANCE;
			var dictBase = hc4->dictBase;
			var delta = (int) (ip - iLowLimit);
			var nbAttempts = maxNbAttempts;
			var pattern = LZ4_read32(ip);
			var repeat = repeat_state_e.rep_untested;
			var srcPatternLength = 0;

			/* First Match */
			LZ4HC_insert(hc4, ip);
			var matchIndex = hashTable[LZ4HC_hashPtr(ip)];

			while (matchIndex >= lowLimit && nbAttempts != 0)
			{
				nbAttempts--;
				if (matchIndex >= dictLimit)
				{
					var matchPtr = basep + matchIndex;
					if (*(iLowLimit + longest) == *(matchPtr - delta + longest))
					{
						if (LZ4_read32(matchPtr) == pattern)
						{
							var mlt = MINMATCH + (int) LZ4_count(ip + MINMATCH, matchPtr + MINMATCH, iHighLimit);
							var back = 0;
							while (
								ip + back > iLowLimit
								&& matchPtr + back > lowPrefixPtr
								&& ip[back - 1] == matchPtr[back - 1])
								back--;

							mlt -= back;

							if (mlt > longest)
							{
								longest = mlt;
								*matchpos = matchPtr + back;
								*startpos = ip + back;
							}
						}
					}
				}
				else
				{
					var matchPtr = dictBase + matchIndex;
					if (LZ4_read32(matchPtr) == pattern)
					{
						var back = 0;
						var vLimit = ip + (dictLimit - matchIndex);
						if (vLimit > iHighLimit) vLimit = iHighLimit;
						var mlt = MINMATCH + (int) LZ4_count(ip + MINMATCH, matchPtr + MINMATCH, vLimit);
						if (ip + mlt == vLimit && vLimit < iHighLimit)
							mlt += (int) LZ4_count(ip + mlt, basep + dictLimit, iHighLimit);

						while (
							ip + back > iLowLimit
							&& matchIndex + back > lowLimit
							&& ip[back - 1] == matchPtr[back - 1])
							back--;

						mlt -= back;
						if (mlt > longest)
						{
							longest = mlt;
							*matchpos = basep + matchIndex + back;
							*startpos = ip + back;
						}
					}
				}

				{
					var nextOffset = DELTANEXTU16(chainTable, (ushort) matchIndex);
					matchIndex -= nextOffset;
					if (patternAnalysis != 0 && nextOffset == 1)
					{
						/* may be a repeated pattern */
						if (repeat == repeat_state_e.rep_untested)
						{
							if (((pattern & 0xFFFF) == pattern >> 16) & ((pattern & 0xFF) == pattern >> 24))
							{
								repeat = repeat_state_e.rep_confirmed;
								srcPatternLength = (int) LZ4HC_countPattern(ip + 4, iHighLimit, pattern) + 4;
							}
							else
							{
								repeat = repeat_state_e.rep_not;
							}
						}

						if (repeat == repeat_state_e.rep_confirmed && matchIndex >= dictLimit)
						{
							var matchPtr = basep + matchIndex;
							if (LZ4_read32(matchPtr) == pattern)
							{
								var forwardPatternLength =
									(int) LZ4HC_countPattern(matchPtr + sizeof(uint), iHighLimit, pattern)
									+ sizeof(uint);
								var maxLowPtr =
									lowPrefixPtr + MAX_DISTANCE >= ip
										? lowPrefixPtr
										: ip - MAX_DISTANCE;
								var backLength = (int) LZ4HC_reverseCountPattern(matchPtr, maxLowPtr, pattern);
								var currentSegmentLength = backLength + forwardPatternLength;

								if (currentSegmentLength >= srcPatternLength && forwardPatternLength <= srcPatternLength)
								{
									matchIndex += (uint) (forwardPatternLength - srcPatternLength);
								}
								else
								{
									matchIndex -= (uint) backLength;
								}
							}
						}
					}
				}
			}

			return longest;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		static int LZ4HC_InsertAndFindBestMatch(
			LZ4HC_CCtx_t* hc4,
			byte* ip, byte* iLimit,
			byte** matchpos,
			int maxNbAttempts,
			int patternAnalysis)
		{
			var uselessPtr = ip;
			return LZ4HC_InsertAndGetWiderMatch(
				hc4,
				ip,
				ip,
				iLimit,
				MINMATCH - 1,
				matchpos,
				&uselessPtr,
				maxNbAttempts,
				patternAnalysis);
		}

		enum limitedOutput_directive
		{
			noLimit = 0,
			limitedOutput = 1,
			limitedDestSize = 2,
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		static int LZ4HC_encodeSequence(
			byte** ip,
			byte** op,
			byte** anchor,
			int matchLength,
			byte* match,
			limitedOutput_directive limit,
			byte* oend)
		{
			var token = (*op)++;

			var length = (size_t) (*ip - *anchor);
			if (limit != limitedOutput_directive.noLimit
				&& *op + (length >> 8) + length + (2 + 1 + LASTLITERALS) > oend)
				return 1;

			if (length >= RUN_MASK)
			{
				var len = length - RUN_MASK;
				*token = (byte) (RUN_MASK << ML_BITS);
				for (; len >= 255; len -= 255) *(*op)++ = 255;
				*(*op)++ = (byte) len;
			}
			else
			{
				*token = (byte) (length << ML_BITS);
			}

			Mem.WildCopy(*op, *anchor, (*op) + length);

			*op += length;
			LZ4_write16(*op, (ushort) (*ip - match));
			*op += 2;

			length = (size_t) (matchLength - MINMATCH);
			if (limit != limitedOutput_directive.noLimit && *op + (length >> 8) + (1 + LASTLITERALS) > oend)
				return 1;

			if (length >= ML_MASK)
			{
				*token += (byte) ML_MASK;
				length -= ML_MASK;
				for (; length >= 510; length -= 510)
				{
					*(*op)++ = 255;
					*(*op)++ = 255;
				}

				if (length >= 255)
				{
					length -= 255;
					*(*op)++ = 255;
				}

				*(*op)++ = (byte) length;
			}
			else
			{
				*token += (byte) (length);
			}

			*ip += matchLength;
			*anchor = *ip;
			return 0;
		}

		const int LZ4_OPT_NUM = (1 << 12);

		struct LZ4HC_optimal_t
		{
			public int price;
			public int off;
			public int mlen;
			public int litlen;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		static int LZ4HC_literalsPrice(int litlen) =>
			litlen >= (int) RUN_MASK
				? litlen + (int) (1 + (litlen - RUN_MASK) / 255)
				: litlen;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		static int LZ4HC_sequencePrice(int litlen, int mlen)
		{
			var price = 1 + 2 + LZ4HC_literalsPrice(litlen);
			return mlen >= (int) (ML_MASK + MINMATCH)
				? price + (int) (1 + (mlen - (ML_MASK + MINMATCH)) / 255)
				: price;
		}

		struct LZ4HC_match_t
		{
			public int off;
			public int len;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		static LZ4HC_match_t LZ4HC_FindLongerMatch(
			LZ4HC_CCtx_t* ctx, byte* ip, byte* iHighLimit, int minLen, int nbSearches)
		{
			LZ4HC_match_t match;
			Mem.Zero((byte*) &match, sizeof(LZ4HC_match_t));
			byte* matchPtr = null;
			var matchLength =
				LZ4HC_InsertAndGetWiderMatch(ctx, ip, ip, iHighLimit, minLen, &matchPtr, &ip, nbSearches, 1);
			if (matchLength <= minLen) return match;

			match.len = matchLength;
			match.off = (int) (ip - matchPtr);
			return match;
		}

		//----

		static int LZ4HC_compress_optimal(
			LZ4HC_CCtx_t* ctx,
			byte* source,
			byte* dst,
			int* srcSizePtr,
			int dstCapacity,
			int nbSearches,
			size_t sufficient_len,
			limitedOutput_directive limit,
			int fullUpdate)
		{
			const int TRAILING_LITERALS = 3;

			var opt = stackalloc LZ4HC_optimal_t[LZ4_OPT_NUM + TRAILING_LITERALS];

			byte* ip = (byte*) source;
			byte* anchor = ip;
			byte* iend = ip + *srcSizePtr;
			byte* mflimit = iend - MFLIMIT;
			byte* matchlimit = iend - LASTLITERALS;
			byte* op = (byte*) dst;
			byte* opSaved = (byte*) dst;
			byte* oend = op + dstCapacity;

			*srcSizePtr = 0;
			if (limit == limitedOutput_directive.limitedDestSize) oend -= LASTLITERALS;
			if (sufficient_len >= LZ4_OPT_NUM) sufficient_len = LZ4_OPT_NUM - 1;

			/* Main Loop */
			while (ip < mflimit)
			{
				int llen = (int) (ip - anchor);
				int best_mlen, best_off;
				int cur, last_match_pos = 0;

				LZ4HC_match_t firstMatch = LZ4HC_FindLongerMatch(ctx, ip, matchlimit, MINMATCH - 1, nbSearches);
				if (firstMatch.len == 0)
				{
					ip++;
					continue;
				}

				if ((size_t) firstMatch.len > sufficient_len)
				{
					int firstML = firstMatch.len;
					byte* matchPos = ip - firstMatch.off;
					opSaved = op;
					if (LZ4HC_encodeSequence(&ip, &op, &anchor, firstML, matchPos, limit, oend) != 0)
						goto _dest_overflow;

					continue;
				}

				/* set prices for first positions (literals) */
				{
					int rPos;
					for (rPos = 0; rPos < MINMATCH; rPos++)
					{
						int cost = LZ4HC_literalsPrice(llen + rPos);
						opt[rPos].mlen = 1;
						opt[rPos].off = 0;
						opt[rPos].litlen = llen + rPos;
						opt[rPos].price = cost;
					}
				}
				/* set prices using initial match */
				{
					int mlen = MINMATCH;
					int matchML = firstMatch.len; /* necessarily < sufficient_len < LZ4_OPT_NUM */
					int offset = firstMatch.off;
					for (; mlen <= matchML; mlen++)
					{
						int cost = LZ4HC_sequencePrice(llen, mlen);
						opt[mlen].mlen = mlen;
						opt[mlen].off = offset;
						opt[mlen].litlen = llen;
						opt[mlen].price = cost;
					}
				}
				last_match_pos = firstMatch.len;
				{
					int addLit;
					for (addLit = 1; addLit <= TRAILING_LITERALS; addLit++)
					{
						opt[last_match_pos + addLit].mlen = 1;
						opt[last_match_pos + addLit].off = 0;
						opt[last_match_pos + addLit].litlen = addLit;
						opt[last_match_pos + addLit].price = opt[last_match_pos].price + LZ4HC_literalsPrice(addLit);
					}
				}

				/* check further positions */
				for (cur = 1; cur < last_match_pos; cur++)
				{
					byte* curPtr = ip + cur;
					LZ4HC_match_t newMatch;

					if (curPtr >= mflimit) break;

					if (fullUpdate != 0)
					{
						if (opt[cur + 1].price <= opt[cur].price && opt[cur + MINMATCH].price < opt[cur].price + 3)
							continue;
					}
					else
					{
						if (opt[cur + 1].price <= opt[cur].price) continue;
					}

					if (fullUpdate != 0)
						newMatch = LZ4HC_FindLongerMatch(ctx, curPtr, matchlimit, MINMATCH - 1, nbSearches);
					else
						newMatch = LZ4HC_FindLongerMatch(ctx, curPtr, matchlimit, last_match_pos - cur, nbSearches);
					if (newMatch.len == 0) continue;

					if ((size_t) newMatch.len > sufficient_len || newMatch.len + cur >= LZ4_OPT_NUM)
					{
						/* immediate encoding */
						best_mlen = newMatch.len;
						best_off = newMatch.off;
						last_match_pos = cur + 1;
						goto encode;
					}

					/* before match : set price with literals at beginning */
					{
						int baseLitlen = opt[cur].litlen;
						int litlen;
						for (litlen = 1; litlen < MINMATCH; litlen++)
						{
							int price =
								opt[cur].price - LZ4HC_literalsPrice(baseLitlen)
								+ LZ4HC_literalsPrice(baseLitlen + litlen);
							int pos = cur + litlen;
							if (price < opt[pos].price)
							{
								opt[pos].mlen = 1;
								opt[pos].off = 0;
								opt[pos].litlen = baseLitlen + litlen;
								opt[pos].price = price;
							}
						}
					}

					/* set prices using match at position = cur */
					{
						int matchML = newMatch.len;
						int ml = MINMATCH;

						for (; ml <= matchML; ml++)
						{
							int pos = cur + ml;
							int offset = newMatch.off;
							int price;
							int ll;
							if (opt[cur].mlen == 1)
							{
								ll = opt[cur].litlen;
								price =
									((cur > ll) ? opt[cur - ll].price : 0)
									+ LZ4HC_sequencePrice(ll, ml);
							}
							else
							{
								ll = 0;
								price = opt[cur].price + LZ4HC_sequencePrice(0, ml);
							}

							if (pos > last_match_pos + TRAILING_LITERALS || price <= opt[pos].price)
							{
								if (ml == matchML && last_match_pos < pos)
									last_match_pos = pos;
								opt[pos].mlen = ml;
								opt[pos].off = offset;
								opt[pos].litlen = ll;
								opt[pos].price = price;
							}
						}
					}

					{
						int addLit;
						for (addLit = 1; addLit <= TRAILING_LITERALS; addLit++)
						{
							opt[last_match_pos + addLit].mlen = 1;
							opt[last_match_pos + addLit].off = 0;
							opt[last_match_pos + addLit].litlen = addLit;
							opt[last_match_pos + addLit].price = opt[last_match_pos].price + LZ4HC_literalsPrice(addLit);
						}
					}
				}

				best_mlen = opt[last_match_pos].mlen;
				best_off = opt[last_match_pos].off;
				cur = last_match_pos - best_mlen;

				encode: /* cur, last_match_pos, best_mlen, best_off must be set */
				{
					int candidate_pos = cur;
					int selected_matchLength = best_mlen;
					int selected_offset = best_off;
					while (true)
					{
						int next_matchLength = opt[candidate_pos].mlen;
						int next_offset = opt[candidate_pos].off;
						opt[candidate_pos].mlen = selected_matchLength;
						opt[candidate_pos].off = selected_offset;
						selected_matchLength = next_matchLength;
						selected_offset = next_offset;
						if (next_matchLength > candidate_pos) break;

						candidate_pos -= next_matchLength;
					}
				}

				{
					int rPos = 0;
					while (rPos < last_match_pos)
					{
						int ml = opt[rPos].mlen;
						int offset = opt[rPos].off;
						if (ml == 1)
						{
							ip++;
							rPos++;
							continue;
						}

						rPos += ml;
						opSaved = op;
						if (LZ4HC_encodeSequence(&ip, &op, &anchor, ml, ip - offset, limit, oend) != 0)
							goto _dest_overflow;
					}
				}
			}

			_last_literals:
			{
				size_t lastRunSize = (size_t) (iend - anchor);
				size_t litLength = (lastRunSize + 255 - RUN_MASK) / 255;
				size_t totalSize = 1 + litLength + lastRunSize;
				if (limit == limitedOutput_directive.limitedDestSize) oend += LASTLITERALS;
				if (limit != 0 && op + totalSize > oend)
				{
					if (limit == limitedOutput_directive.limitedOutput) return 0;

					lastRunSize = (size_t) (oend - op) - 1;
					litLength = (lastRunSize + 255 - RUN_MASK) / 255;
					lastRunSize -= litLength;
				}

				ip = anchor + lastRunSize;

				if (lastRunSize >= RUN_MASK)
				{
					size_t accumulator = lastRunSize - RUN_MASK;
					*op++ = (byte) (RUN_MASK << ML_BITS);
					for (; accumulator >= 255; accumulator -= 255) *op++ = 255;
					*op++ = (byte) accumulator;
				}
				else
				{
					*op++ = (byte) (lastRunSize << ML_BITS);
				}

				Mem.Copy(op, anchor, (int) lastRunSize);
				op += lastRunSize;
			}

			/* End */
			*srcSizePtr = (int) (((byte*) ip) - source);
			return (int) ((byte*) op - dst);

			_dest_overflow:
			if (limit == limitedOutput_directive.limitedDestSize)
			{
				op = opSaved; /* restore correct out pointer */
				goto _last_literals;
			}

			return 0;
		}

		static int LZ4HC_compress_hashChain(
			LZ4HC_CCtx_t* ctx,
			byte* source,
			byte* dest,
			int* srcSizePtr,
			int maxOutputSize,
			uint maxNbAttempts,
			limitedOutput_directive limit
		)
		{
			int inputSize = *srcSizePtr;
			int patternAnalysis = (maxNbAttempts > 64) ? 1 : 0; /* levels 8+ */

			byte* ip = (byte*) source;
			byte* anchor = ip;
			byte* iend = ip + inputSize;
			byte* mflimit = iend - MFLIMIT;
			byte* matchlimit = (iend - LASTLITERALS);

			byte* optr = (byte*) dest;
			byte* op = (byte*) dest;
			byte* oend = op + maxOutputSize;

			int ml, ml2, ml3, ml0;
			byte* ref_ = null;
			byte* start2 = null;
			byte* ref2 = null;
			byte* start3 = null;
			byte* ref3 = null;
			byte* start0;
			byte* ref0;

			/* init */
			*srcSizePtr = 0;
			if (limit == limitedOutput_directive.limitedDestSize)
				oend -= LASTLITERALS; /* Hack for support LZ4 format restriction */
			if (inputSize < LZ4_minLength)
				goto _last_literals; /* Input too small, no compression (all literals) */

			/* Main Loop */
			while (ip < mflimit)
			{
				ml = LZ4HC_InsertAndFindBestMatch(
					ctx,
					ip,
					matchlimit,
					&ref_,
					(int) maxNbAttempts,
					patternAnalysis);
				if (ml < MINMATCH)
				{
					ip++;
					continue;
				}

				/* saved, in case we would skip too much */
				start0 = ip;
				ref0 = ref_;
				ml0 = ml;

				_Search2:
				if (ip + ml < mflimit)
				{
					ml2 = LZ4HC_InsertAndGetWiderMatch(
						ctx,
						ip + ml - 2,
						ip + 0,
						matchlimit,
						ml,
						&ref2,
						&start2,
						(int) maxNbAttempts,
						patternAnalysis);
				}
				else
				{
					ml2 = ml;
				}

				if (ml2 == ml)
				{
					/* No better match */
					optr = op;
					if (LZ4HC_encodeSequence(&ip, &op, &anchor, ml, ref_, limit, oend) != 0) goto _dest_overflow;

					continue;
				}

				if (start0 < ip)
				{
					if (start2 < ip + ml0)
					{
						/* empirical */
						ip = start0;
						ref_ = ref0;
						ml = ml0;
					}
				}

				/* Here, start0==ip */
				if ((start2 - ip) < 3)
				{
					/* First Match too small : removed */
					ml = ml2;
					ip = start2;
					ref_ = ref2;
					goto _Search2;
				}

				_Search3:
				/* At this stage, we have :
				*  ml2 > ml1, and
				*  ip1+3 <= ip2 (usually < ip1+ml1) */
				if ((start2 - ip) < OPTIMAL_ML)
				{
					int correction;
					int new_ml = ml;
					if (new_ml > OPTIMAL_ML) new_ml = OPTIMAL_ML;
					if (ip + new_ml > start2 + ml2 - MINMATCH) new_ml = (int) (start2 - ip) + ml2 - MINMATCH;
					correction = new_ml - (int) (start2 - ip);
					if (correction > 0)
					{
						start2 += correction;
						ref2 += correction;
						ml2 -= correction;
					}
				}
				/* Now, we have start2 = ip+new_ml, with new_ml = min(ml, OPTIMAL_ML=18) */

				if (start2 + ml2 < mflimit)

					ml3 = LZ4HC_InsertAndGetWiderMatch(
						ctx,
						start2 + ml2 - 3,
						start2,
						matchlimit,
						ml2,
						&ref3,
						&start3,
						(int) maxNbAttempts,
						patternAnalysis);
				else
					ml3 = ml2;

				if (ml3 == ml2)
				{
					/* No better match : 2 sequences to encode */
					/* ip & ref are known; Now for ml */
					if (start2 < ip + ml) ml = (int) (start2 - ip);
					/* Now, encode 2 sequences */
					optr = op;
					if (LZ4HC_encodeSequence(&ip, &op, &anchor, ml, ref_, limit, oend) != 0) goto _dest_overflow;

					ip = start2;
					optr = op;
					if (LZ4HC_encodeSequence(&ip, &op, &anchor, ml2, ref2, limit, oend) != 0) goto _dest_overflow;

					continue;
				}

				if (start3 < ip + ml + 3)
				{
					/* Not enough space for match 2 : remove it */
					if (start3 >= (ip + ml))
					{
						/* can write Seq1 immediately ==> Seq2 is removed, so Seq3 becomes Seq1 */
						if (start2 < ip + ml)
						{
							int correction = (int) (ip + ml - start2);
							start2 += correction;
							ref2 += correction;
							ml2 -= correction;
							if (ml2 < MINMATCH)
							{
								start2 = start3;
								ref2 = ref3;
								ml2 = ml3;
							}
						}

						optr = op;
						if (LZ4HC_encodeSequence(&ip, &op, &anchor, ml, ref_, limit, oend) != 0) goto _dest_overflow;

						ip = start3;
						ref_ = ref3;
						ml = ml3;

						start0 = start2;
						ref0 = ref2;
						ml0 = ml2;
						goto _Search2;
					}

					start2 = start3;
					ref2 = ref3;
					ml2 = ml3;
					goto _Search3;
				}

				/*
				* OK, now we have 3 ascending matches; let's write at least the first one
				* ip & ref are known; Now for ml
				*/
				if (start2 < ip + ml)
				{
					if ((start2 - ip) < (int) ML_MASK)
					{
						int correction;
						if (ml > OPTIMAL_ML) ml = OPTIMAL_ML;
						if (ip + ml > start2 + ml2 - MINMATCH) ml = (int) (start2 - ip) + ml2 - MINMATCH;
						correction = ml - (int) (start2 - ip);
						if (correction > 0)
						{
							start2 += correction;
							ref2 += correction;
							ml2 -= correction;
						}
					}
					else
					{
						ml = (int) (start2 - ip);
					}
				}

				optr = op;
				if (LZ4HC_encodeSequence(&ip, &op, &anchor, ml, ref_, limit, oend) != 0) goto _dest_overflow;

				ip = start2;
				ref_ = ref2;
				ml = ml2;

				start2 = start3;
				ref2 = ref3;
				ml2 = ml3;

				goto _Search3;
			}

			_last_literals:
			/* Encode Last Literals */
			{
				size_t lastRunSize = (size_t) (iend - anchor); /* literals */
				size_t litLength = (lastRunSize + 255 - RUN_MASK) / 255;
				size_t totalSize = 1 + litLength + lastRunSize;
				if (limit == limitedOutput_directive.limitedDestSize)
					oend += LASTLITERALS; /* restore correct value */
				if (limit != 0 && (op + totalSize > oend))
				{
					if (limit == limitedOutput_directive.limitedOutput) return 0; /* Check output limit */

					/* adapt lastRunSize to fill 'dest' */
					lastRunSize = (size_t) (oend - op) - 1;
					litLength = (lastRunSize + 255 - RUN_MASK) / 255;
					lastRunSize -= litLength;
				}

				ip = anchor + lastRunSize;

				if (lastRunSize >= RUN_MASK)
				{
					size_t accumulator = lastRunSize - RUN_MASK;

					*op++ = (byte) (RUN_MASK << ML_BITS);
					for (; accumulator >= 255; accumulator -= 255) *op++ = 255;

					*op++ = (byte) accumulator;
				}
				else
				{
					*op++ = (byte) (lastRunSize << ML_BITS);
				}

				Mem.Copy(op, anchor, (int) lastRunSize);
				op += lastRunSize;
			}

			/* End */
			*srcSizePtr = (int) (((byte*) ip) - source);
			return (int) (((byte*) op) - dest);

			_dest_overflow:
			if (limit == limitedOutput_directive.limitedDestSize)
			{
				op = optr; /* restore correct out pointer */
				goto _last_literals;
			}

			return 0;
		}

		enum lz4hc_strat_e
		{
			lz4hc,
			lz4opt
		};

		struct cParams_t
		{
			public lz4hc_strat_e strat;
			public uint32 nbSearches;
			public uint32 targetLength;

			public cParams_t(lz4hc_strat_e strat, uint32 nbSearches, uint32 targetLength)
			{
				this.strat = strat;
				this.nbSearches = nbSearches;
				this.targetLength = targetLength;
			}
		}

		static cParams_t[] clTable = {
			new cParams_t(lz4hc_strat_e.lz4hc, 2, 16), /* 0, unused */
			new cParams_t(lz4hc_strat_e.lz4hc, 2, 16), /* 1, unused */
			new cParams_t(lz4hc_strat_e.lz4hc, 2, 16), /* 2, unused */
			new cParams_t(lz4hc_strat_e.lz4hc, 4, 16), /* 3 */
			new cParams_t(lz4hc_strat_e.lz4hc, 8, 16), /* 4 */
			new cParams_t(lz4hc_strat_e.lz4hc, 16, 16), /* 5 */
			new cParams_t(lz4hc_strat_e.lz4hc, 32, 16), /* 6 */
			new cParams_t(lz4hc_strat_e.lz4hc, 64, 16), /* 7 */
			new cParams_t(lz4hc_strat_e.lz4hc, 128, 16), /* 8 */
			new cParams_t(lz4hc_strat_e.lz4hc, 256, 16), /* 9 */
			new cParams_t(lz4hc_strat_e.lz4opt, 96, 64), /* 10==LZ4HC_CLEVEL_OPT_MIN*/
			new cParams_t(lz4hc_strat_e.lz4opt, 512, 128), /* 11 */
			new cParams_t(lz4hc_strat_e.lz4opt, 8192, LZ4_OPT_NUM), /* 12==LZ4HC_CLEVEL_MAX */
		};

		static int LZ4HC_compress_generic(
			LZ4HC_CCtx_t* ctx,
			byte* src,
			byte* dst,
			int* srcSizePtr,
			int dstCapacity,
			int cLevel,
			limitedOutput_directive limit)
		{
			if (limit == limitedOutput_directive.limitedDestSize && dstCapacity < 1)
				return 0; /* Impossible to store anything */
			if ((uint) *srcSizePtr > (uint) LZ4_MAX_INPUT_SIZE)
				return 0; /* Unsupported input size (too large or negative) */

			ctx->end += *srcSizePtr;
			if (cLevel < 1)
				cLevel = LZ4HC_CLEVEL_DEFAULT; /* note : convention is different from lz4frame, maybe something to review */
			cLevel = Math.Min(LZ4HC_CLEVEL_MAX, cLevel);

			var cParam = clTable[cLevel];
			if (cParam.strat == lz4hc_strat_e.lz4hc)
				return LZ4HC_compress_hashChain(
					ctx,
					src,
					dst,
					srcSizePtr,
					dstCapacity,
					cParam.nbSearches,
					limit);

			return LZ4HC_compress_optimal(
				ctx,
				src,
				dst,
				srcSizePtr,
				dstCapacity,
				(int) cParam.nbSearches,
				cParam.targetLength,
				limit,
				cLevel == LZ4HC_CLEVEL_MAX ? 1 : 0); /* ultra mode */
		}

		static int LZ4_compress_HC_extStateHC(
			LZ4HC_CCtx_t* ctx, byte* src, byte* dst, int srcSize, int dstCapacity, int compressionLevel)
		{
			if (((size_t) ctx & (size_t) (sizeof(void*) - 1)) != 0)
				return 0;

			LZ4HC_init(ctx, src);

			return LZ4HC_compress_generic(
				ctx,
				src,
				dst,
				&srcSize,
				dstCapacity,
				compressionLevel,
				dstCapacity < LZ4_compressBound(srcSize)
					? limitedOutput_directive.limitedOutput
					: limitedOutput_directive.noLimit);
		}

		internal static int LZ4_compress_HC(
			byte* src, byte* dst, int srcSize, int dstCapacity, int compressionLevel)
		{
			var statePtr = (LZ4HC_CCtx_t*) Mem.Alloc(sizeof(LZ4HC_CCtx_t));
			try
			{
				return LZ4_compress_HC_extStateHC(statePtr, src, dst, srcSize, dstCapacity, compressionLevel);
			}
			finally
			{
				Mem.Free(statePtr);
			}
		}

		static int LZ4_compress_HC_destSize(
			LZ4HC_CCtx_t* ctx, byte* source, byte* dest, int* sourceSizePtr, int targetDestSize, int cLevel)
		{
			LZ4HC_init(ctx, source);
			return LZ4HC_compress_generic(
				ctx,
				source,
				dest,
				sourceSizePtr,
				targetDestSize,
				cLevel,
				limitedOutput_directive.limitedDestSize);
		}
	}
}
