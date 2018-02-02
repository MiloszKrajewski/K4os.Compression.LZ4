using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace K4os.Compression.LZ4
{
	internal unsafe class LZ4_xx
	{
		// [StructLayout(LayoutKind.Sequential)]
		// [MethodImpl(MethodImplOptions.AggressiveInlining)]

		protected const int LZ4_MEMORY_USAGE = 14;
		protected const int LZ4_MAX_INPUT_SIZE = 0x7E000000;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal static int LZ4_compressBound(int isize) =>
			isize > LZ4_MAX_INPUT_SIZE ? 0 : isize + isize / 255 + 16;

		protected const int LZ4_HASHLOG = LZ4_MEMORY_USAGE - 2;
		protected const int LZ4_HASHTABLESIZE = 1 << LZ4_MEMORY_USAGE;
		protected const int LZ4_HASH_SIZE_U32 = 1 << LZ4_HASHLOG;

		[StructLayout(LayoutKind.Sequential)]
		internal struct LZ4_stream_t
		{	
			public fixed uint hashTable[LZ4_HASH_SIZE_U32];
			public uint currentOffset;
			public uint initCheck;
			public byte* dictionary;
			public byte* bufferStart; /* obsolete, used for slideInputBuffer */
			public uint dictSize;
		};

		[StructLayout(LayoutKind.Sequential)]
		internal struct LZ4_streamDecode_t
		{
			public byte* externalDict;
			public uint extDictSize;
			public byte* prefixEnd;
			public uint prefixSize;
		};

		//protected const int LZ4_STREAMSIZE_U64 = (1 << (LZ4_MEMORY_USAGE - 3)) + 4;
		//protected const int LZ4_STREAMSIZE = LZ4_STREAMSIZE_U64 * sizeof(ulong);

		//protected const int LZ4_STREAMDECODESIZE_U64 = 4;
		//protected const int LZ4_STREAMDECODESIZE = LZ4_STREAMDECODESIZE_U64 * sizeof(ulong);

		//protected const int LZ4_HEAPMODE = 0;

		protected const int ACCELERATION_DEFAULT = 1;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		protected static ushort LZ4_read16(void* p) => *(ushort*) p;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		protected static uint LZ4_read32(void* p) => *(uint*) p;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		protected static void LZ4_write16(void* p, ushort v) => *(ushort*) p = v;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		protected static void LZ4_write32(void* p, uint v) => *(uint*) p = v;

		protected const int MINMATCH = 4;

		protected const int WILDCOPYLENGTH = 8;
		protected const int LASTLITERALS = 5;
		protected const int MFLIMIT = WILDCOPYLENGTH + MINMATCH;
		protected const int LZ4_minLength = MFLIMIT + 1;

		protected const int KB = 1 << 10;
		protected const int MB = 1 << 20;
		protected const int GB = 1 << 30;

		protected const int MAXD_LOG = 16;
		protected const int MAX_DISTANCE = (1 << MAXD_LOG) - 1;

		protected const int ML_BITS = 4;
		protected const uint ML_MASK = (1U << ML_BITS) - 1;
		protected const int RUN_BITS = 8 - ML_BITS;
		protected const uint RUN_MASK = (1U << RUN_BITS) - 1;

		protected const int LZ4_64Klimit = 64 * KB + (MFLIMIT - 1);
		protected const int LZ4_skipTrigger = 6;

		public enum limitedOutput_directive
		{
			notLimited = 0,
			limitedOutput = 1
		}

		public enum tableType_t
		{
			byPtr = 0,
			byU32 = 1,
			byU16 = 2
		}

		public enum dict_directive
		{
			noDict = 0,
			withPrefix64k,
			usingExtDict
		}

		public enum dictIssue_directive
		{
			noDictIssue = 0,
			dictSmall
		}

		public enum endCondition_directive
		{
			endOnOutputSize = 0,
			endOnInputSize = 1
		}

		public enum earlyEnd_directive
		{
			full = 0,
			partial = 1
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		protected static uint LZ4_hash4(uint sequence, tableType_t tableType)
		{
			var hashLog = tableType == tableType_t.byU16 ? LZ4_HASHLOG + 1 : LZ4_HASHLOG;
			return (sequence * 2654435761U) >> (MINMATCH * 8 - hashLog);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		protected static uint LZ4_hash5(ulong sequence, tableType_t tableType)
		{
			var hashLog = tableType == tableType_t.byU16 ? LZ4_HASHLOG + 1 : LZ4_HASHLOG;
			return (uint) (((sequence << 24) * 889523592379ul) >> (64 - hashLog));
		}
	}
}
