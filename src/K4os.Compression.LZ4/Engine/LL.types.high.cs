// ReSharper disable IdentifierTypo
// ReSharper disable InconsistentNaming

using System;
using System.Runtime.InteropServices;

namespace K4os.Compression.LZ4.Engine
{
	internal unsafe partial class LL
	{
		protected enum dictCtx_directive { noDictCtx = 0, usingDictCtxHc }

		protected const int LZ4HC_DICTIONARY_LOGSIZE = 16;
		protected const int LZ4HC_MAXD = (1 << LZ4HC_DICTIONARY_LOGSIZE);
		protected const int LZ4HC_MAXD_MASK = (LZ4HC_MAXD - 1);

		protected const int LZ4HC_HASH_LOG = 15;
		protected const int LZ4HC_HASHTABLESIZE = (1 << LZ4HC_HASH_LOG);
		protected const int LZ4HC_HASH_MASK = (LZ4HC_HASHTABLESIZE - 1);

		[StructLayout(LayoutKind.Sequential)]
		public struct LZ4HC_CCtx_t
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
			public LZ4HC_CCtx_t* dictCtx;
		}
	}
}
