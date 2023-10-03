// ReSharper disable IdentifierTypo
// ReSharper disable InconsistentNaming
// ReSharper disable AccessToStaticMemberViaDerivedType
// ReSharper disable ConditionIsAlwaysTrueOrFalse
// ReSharper disable BuiltInTypeReferenceStyle

using System;
using System.Runtime.InteropServices;

using size_t = System.UInt32;
using uptr_t = System.UInt64;

namespace K4os.Compression.LZ4.Engine;

internal unsafe partial class LL
{
	public enum dictCtx_directive { noDictCtx = 0, usingDictCtxHc }

	protected const int LZ4HC_DICTIONARY_LOGSIZE = 16;
	protected const int LZ4HC_MAXD = (1 << LZ4HC_DICTIONARY_LOGSIZE);
	protected const int LZ4HC_MAXD_MASK = (LZ4HC_MAXD - 1);

	protected const int LZ4HC_HASH_LOG = 15;
	protected const int LZ4HC_HASHTABLESIZE = (1 << LZ4HC_HASH_LOG);
	protected const int LZ4HC_HASH_MASK = (LZ4HC_HASHTABLESIZE - 1);

	protected const int LZ4HC_CLEVEL_MIN = 3;
	protected const int LZ4HC_CLEVEL_DEFAULT = 9;
	protected const int LZ4HC_CLEVEL_OPT_MIN = 10;
	protected const int LZ4HC_CLEVEL_MAX = 12;

	[StructLayout(LayoutKind.Sequential)]
	public struct LZ4_streamHC_t
	{
		public fixed uint hashTable[LZ4HC_HASHTABLESIZE];
		public fixed ushort chainTable[LZ4HC_MAXD];
		public byte* end; /* next block here to continue on current prefix */
		public byte* @base; /* All index relative to this position */
		public byte* dictBase; /* alternate @base for extDict */
		public uint dictLimit; /* below that point, need extDict */
		public uint lowLimit; /* below that point, no more dict */
		public uint nextToUpdate; /* index from which to continue dictionary update */
		public short compressionLevel;
		public bool favorDecSpeed; /* favor decompression speed if this flag set */
		public bool dirty; /* stream has to be fully reset if this flag is set */
		public LZ4_streamHC_t* dictCtx;
	}

	protected enum repeat_state_e { rep_untested, rep_not, rep_confirmed }

	public enum HCfavor_e { favorCompressionRatio = 0, favorDecompressionSpeed }

	[StructLayout(LayoutKind.Sequential)]
	public struct LZ4HC_match_t
	{
		public int off;
		public int len;
	}

	[StructLayout(LayoutKind.Sequential)]
	public struct LZ4HC_optimal_t
	{
		public int price;
		public int off;
		public int mlen;
		public int litlen;
	}

	public enum lz4hc_strat_e { lz4hc, lz4opt }

	[StructLayout(LayoutKind.Sequential)]
	public struct cParams_t
	{
		public lz4hc_strat_e strat;
		public uint nbSearches;
		public uint targetLength;

		public cParams_t(lz4hc_strat_e strat, uint nbSearches, uint targetLength)
		{
			this.strat = strat;
			this.nbSearches = nbSearches;
			this.targetLength = targetLength;
		}
	}
}