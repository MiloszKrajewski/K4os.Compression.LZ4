using System.Runtime.CompilerServices;
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
	}

	enum limitedOutput_directive
	{
		noLimit = 0,
		limitedOutput = 1,
		limitedDestSize = 2,
	}
}
