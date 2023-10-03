// ReSharper disable IdentifierTypo
// ReSharper disable InconsistentNaming
// ReSharper disable AccessToStaticMemberViaDerivedType
// ReSharper disable ConditionIsAlwaysTrueOrFalse
// ReSharper disable BuiltInTypeReferenceStyle
// ReSharper disable CommentTypo

using System;
using System.Runtime.InteropServices;

using size_t = System.UInt32;
using uptr_t = System.UInt64;

namespace K4os.Compression.LZ4.Engine
{
	internal unsafe partial class LL
	{
		protected const int LZ4_MEMORY_USAGE = 14;
		protected const int LZ4_MAX_INPUT_SIZE = 0x7E000000;
		protected const int LZ4_DISTANCE_MAX = 65535;
		protected const int LZ4_DISTANCE_ABSOLUTE_MAX = 65535;

		protected const int LZ4_HASHLOG = LZ4_MEMORY_USAGE - 2;
		protected const int LZ4_HASHTABLESIZE = 1 << LZ4_MEMORY_USAGE;
		protected const int LZ4_HASH_SIZE_U32 = 1 << LZ4_HASHLOG;

		protected const int ACCELERATION_DEFAULT = 1;

		[StructLayout(LayoutKind.Sequential)]
		public struct LZ4_stream_t
		{
			public fixed uint hashTable[LZ4_HASH_SIZE_U32];
			public uint currentOffset;
			public bool dirty;
			public tableType_t tableType;
			public byte* dictionary;
			public LZ4_stream_t* dictCtx;
			public uint dictSize;
		}

		[StructLayout(LayoutKind.Sequential)]
		public struct LZ4_streamDecode_t
		{
			public byte* externalDict;
			public uint extDictSize;
			public byte* prefixEnd;
			public uint prefixSize;
		}

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
		
		protected const int OPTIMAL_ML = (int) ((ML_MASK - 1) + MINMATCH);
		protected const int LZ4_OPT_NUM = (1 << 12);

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

		protected enum variable_length_error
		{
			loop_error = -2, initial_error = -1, ok = 0
		}
	}
}
