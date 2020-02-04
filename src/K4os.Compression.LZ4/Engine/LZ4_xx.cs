using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using K4os.Compression.LZ4.Internal;

// ReSharper disable IdentifierTypo
// ReSharper disable InconsistentNaming

namespace K4os.Compression.LZ4.Engine
{
	internal unsafe class LZ4_xx
	{
		// [StructLayout(LayoutKind.Sequential)]
		// [MethodImpl(MethodImplOptions.AggressiveInlining)]

		protected const int LZ4_MEMORY_USAGE = 14;
		protected const int LZ4_MAX_INPUT_SIZE = 0x7E000000;
		protected const int LZ4_DISTANCE_MAX = 65535;
		protected const int LZ4_DISTANCE_ABSOLUTE_MAX = 65535;

		protected const int LZ4_HASHLOG = LZ4_MEMORY_USAGE - 2;
		protected const int LZ4_HASHTABLESIZE = 1 << LZ4_MEMORY_USAGE;
		protected const int LZ4_HASH_SIZE_U32 = 1 << LZ4_HASHLOG;

		protected const int ACCELERATION_DEFAULT = 1;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal static int LZ4_compressBound(int isize) =>
			isize > LZ4_MAX_INPUT_SIZE ? 0 : isize + isize / 255 + 16;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal static int LZ4_decoderRingBufferSize(int isize) =>
			65536 + 14 + isize;

		[StructLayout(LayoutKind.Sequential)]
		internal struct LZ4_stream_t
		{
			public fixed uint hashTable[LZ4_HASH_SIZE_U32];
			public uint currentOffset;
			public bool dirty;
			public tableType_t tableType;
			public byte* dictionary;
			public LZ4_stream_t* dictCtx;
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

		protected const int MINMATCH = 4;

		protected const int WILDCOPYLENGTH = 8;
		protected const int LASTLITERALS = 5;
		protected const int MFLIMIT = 12; // WILDCOPYLENGTH + MINMATCH;

		protected const int MATCH_SAFEGUARD_DISTANCE = 2 * WILDCOPYLENGTH - MINMATCH;
		protected const int FASTLOOP_SAFE_DISTANCE = 64;

		protected const int LZ4_minLength = MFLIMIT + 1;

		protected const int KB = 1 << 10;
		protected const int MB = 1 << 20;
		protected const uint GB = 1u << 30;

		//???
		// protected const int MAXD_LOG = 16;
		// protected const int MAX_DISTANCE = (1 << MAXD_LOG) - 1;

		protected const int ML_BITS = 4;
		protected const uint ML_MASK = (1U << ML_BITS) - 1;
		protected const int RUN_BITS = 8 - ML_BITS;
		protected const uint RUN_MASK = (1U << RUN_BITS) - 1;

		protected const int LZ4_64Klimit = 64 * KB + (MFLIMIT - 1);
		protected const int LZ4_skipTrigger = 6;

		public enum limitedOutput_directive
		{
			notLimited = 0, limitedOutput = 1, fillOutput = 2
		}

		public enum tableType_t
		{
			clearedTable = 0, byPtr, byU32, byU16
		}

		public enum dict_directive
		{
			noDict = 0, withPrefix64k, usingExtDict, usingDictCtx
		}

		public enum dictIssue_directive
		{
			noDictIssue = 0, dictSmall
		}

		public enum endCondition_directive
		{
			endOnOutputSize = 0, endOnInputSize = 1
		}

		public enum earlyEnd_directive
		{
			full = 0, partial = 1
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		protected static uint LZ4_hash4(uint sequence, tableType_t tableType)
		{
			var hashLog = tableType == tableType_t.byU16 ? LZ4_HASHLOG + 1 : LZ4_HASHLOG;
			return unchecked((sequence * 2654435761u) >> (MINMATCH * 8 - hashLog));
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		protected static uint LZ4_hash5(ulong sequence, tableType_t tableType)
		{
			var hashLog = tableType == tableType_t.byU16 ? LZ4_HASHLOG + 1 : LZ4_HASHLOG;
			return unchecked((uint) (((sequence << 24) * 889523592379ul) >> (64 - hashLog)));
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		protected static void LZ4_clearHash(uint h, void* tableBase, tableType_t tableType)
		{
			switch (tableType)
			{
				case tableType_t.byPtr:
					((byte**) tableBase)[h] = null;
					return;
				case tableType_t.byU32:
					((uint*) tableBase)[h] = 0;
					return;
				case tableType_t.byU16:
					((ushort*) tableBase)[h] = 0;
					return;
				default:
					Debug.Assert(false);
					return;
			}
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		protected static void LZ4_putIndexOnHash(
			uint idx, uint h, void* tableBase, tableType_t tableType)
		{
			switch (tableType)
			{
				case tableType_t.byU32:
					((uint*) tableBase)[h] = idx;
					return;
				case tableType_t.byU16:
					Debug.Assert(idx < 65536);
					((ushort*) tableBase)[h] = (ushort) idx;
					return;
				default:
					Debug.Assert(false);
					return;
			}
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		protected static void LZ4_putPositionOnHash(
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
				default:
					Debug.Assert(false);
					return;
			}
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		protected static uint LZ4_getIndexOnHash(uint h, void* tableBase, tableType_t tableType)
		{
			Debug.Assert(LZ4_MEMORY_USAGE > 2);
			switch (tableType)
			{
				case tableType_t.byU32:
					Debug.Assert(h < (1U << (LZ4_MEMORY_USAGE - 2)));
					return ((uint*) tableBase)[h];
				case tableType_t.byU16:
					Debug.Assert(h < (1U << (LZ4_MEMORY_USAGE - 1)));
					return ((ushort*) tableBase)[h];
				default:
					Debug.Assert(false);
					return 0;
			}
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		protected static byte* LZ4_getPositionOnHash(
			uint h, void* tableBase, tableType_t tableType, byte* srcBase)
		{
			switch (tableType)
			{
				case tableType_t.byPtr: return ((byte**) tableBase)[h];
				case tableType_t.byU32: return ((uint*) tableBase)[h] + srcBase;
				default: return ((ushort*) tableBase)[h] + srcBase;
			}
		}

		private static readonly uint[] inc32table = { 0, 1, 2, 1, 0, 4, 4, 4 };
		private static readonly int[] dec64table = { 0, 0, 0, -1, -4, 1, 2, 3 };
	}
}
