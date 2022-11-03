﻿using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using K4os.Compression.LZ4.Internal;

//------------------------------------------------------------------------------

// ReSharper disable IdentifierTypo
// ReSharper disable InconsistentNaming
// ReSharper disable AccessToStaticMemberViaDerivedType
// ReSharper disable ConditionIsAlwaysTrueOrFalse
// ReSharper disable BuiltInTypeReferenceStyle
using size_t = System.UInt32;
using uptr_t = System.UInt64;

//------------------------------------------------------------------------------

namespace K4os.Compression.LZ4.Engine
{
	internal unsafe partial class LL
	{
		// [StructLayout(LayoutKind.Sequential)]
		// [MethodImpl(MethodImplOptions.AggressiveInlining)]

		[Conditional("DEBUG")]
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void Assert(bool condition, string message = null)
		{
			if (!condition)
				throw new ArgumentException(message ?? "Assert failed");
		}

		public static bool Enforce32 { get; set; } = false;

		/// <summary>Checks what algorithm should be used (32 vs 64 bit).</summary>
		public static Algorithm Algorithm
		{
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get => Enforce32 || Mem.System32 ? Algorithm.X32 : Algorithm.X64;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal static int LZ4_compressBound(int isize) =>
			isize > LZ4_MAX_INPUT_SIZE ? 0 : isize + isize / 255 + 16;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal static int LZ4_decoderRingBufferSize(int isize) =>
			65536 + 14 + isize;

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
					Assert(false);
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
					Assert(idx < 65536);
					((ushort*) tableBase)[h] = (ushort) idx;
					return;
				default:
					Assert(false);
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
					Assert(false);
					return;
			}
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		protected static uint LZ4_getIndexOnHash(uint h, void* tableBase, tableType_t tableType)
		{
			Assert(LZ4_MEMORY_USAGE > 2);
			switch (tableType)
			{
				case tableType_t.byU32:
					Assert(h < (1U << (LZ4_MEMORY_USAGE - 2)));
					return ((uint*) tableBase)[h];
				case tableType_t.byU16:
					Assert(h < (1U << (LZ4_MEMORY_USAGE - 1)));
					return ((ushort*) tableBase)[h];
				default:
					Assert(false);
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
		
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static int MIN(int a, int b) => a < b ? a : b;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static uint MIN(uint a, uint b) => a < b ? a : b;
		
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static uint MAX(uint a, uint b) => a < b ? b : a;
		
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static long MAX(long a, long b) => a < b ? b : a;
		
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static long MIN(long a, long b) => a < b ? a : b;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		protected static uint LZ4_readVLE(
			byte** ip, byte* lencheck,
			bool loop_check, bool initial_check,
			variable_length_error* error)
		{
			uint length = 0;
			uint s;
			if (initial_check && ((*ip) >= lencheck))
			{
				*error = variable_length_error.initial_error;
				return length;
			}

			do
			{
				s = **ip;
				(*ip)++;
				length += s;
				if (loop_check && ((*ip) >= lencheck))
				{
					*error = variable_length_error.loop_error;
					return length;
				}
			}
			while (s == 255);

			return length;
		}

		public static int LZ4_saveDict(LZ4_stream_t* LZ4_dict, byte* safeBuffer, int dictSize)
		{
			var dict = LZ4_dict;
			var previousDictEnd = dict->dictionary + dict->dictSize;

			if ((uint) dictSize > 64 * KB)
			{
				dictSize = 64 * KB;
			}

			if ((uint) dictSize > dict->dictSize) dictSize = (int) dict->dictSize;

			Mem.Move(safeBuffer, previousDictEnd - dictSize, dictSize);

			dict->dictionary = safeBuffer;
			dict->dictSize = (uint) dictSize;

			return dictSize;
		}

		/*
		Memory allocation has been moved to array pool, but I keep these methods for reference.
		 
		public static LZ4_stream_t* LZ4_createStream() =>
			(LZ4_stream_t*) Mem.AllocZero(sizeof(LZ4_stream_t));

		public static void LZ4_freeStream(LZ4_stream_t* LZ4_stream)
		{
			if (LZ4_stream != null) Mem.Free(LZ4_stream);
		}

		public static LZ4_streamDecode_t* LZ4_createStreamDecode() =>
			(LZ4_streamDecode_t*) Mem.AllocZero(sizeof(LZ4_streamDecode_t));

		public static void LZ4_freeStreamDecode(LZ4_streamDecode_t* LZ4_stream)
		{
			if (LZ4_stream != null) Mem.Free(LZ4_stream);
		}
		*/

		public static LZ4_stream_t* LZ4_initStream(LZ4_stream_t* buffer)
		{
			Mem.Zero((byte*) buffer, sizeof(LZ4_stream_t));
			return buffer;
		}

		public static void LZ4_setStreamDecode(
			LZ4_streamDecode_t* LZ4_streamDecode, byte* dictionary, int dictSize)
		{
			var lz4sd = LZ4_streamDecode;
			lz4sd->prefixSize = (uint) dictSize;
			lz4sd->prefixEnd = dictionary + dictSize;
			lz4sd->externalDict = null;
			lz4sd->extDictSize = 0;
		}
		
		private static readonly uint[] _inc32table = { 0, 1, 2, 1, 0, 4, 4, 4 };
		private static readonly int[] _dec64table = { 0, 0, 0, -1, -4, 1, 2, 3 };

		protected static readonly uint* inc32table = Mem.CloneArray(_inc32table);
		protected static readonly int* dec64table = Mem.CloneArray(_dec64table);
	}
}
