#pragma warning disable 1591
// ReSharper disable IdentifierTypo
// ReSharper disable InconsistentNaming

using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using K4os.Compression.LZ4.Internal;

namespace K4os.Compression.LZ4.Engine
{
	public unsafe class LLTools: LLTypes
	{
		// [StructLayout(LayoutKind.Sequential)]
		// [MethodImpl(MethodImplOptions.AggressiveInlining)]
		
		public static bool Force32Bit { get; set; }

		/// <summary>Checks if process is ran in 32-bit mode.</summary>
		public static bool Algorithm32
		{
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get => Force32Bit || Mem.System32;
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

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static uint LZ4_min(uint a, uint b) => a < b ? a : b;

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

		public static LZ4_stream_t* LZ4_createStream() =>
			(LZ4_stream_t*) Mem.AllocZero(sizeof(LZ4_stream_t));

		public static LZ4_stream_t* LZ4_initStream(LZ4_stream_t* buffer)
		{
			Mem.Zero((byte*) buffer, sizeof(LZ4_stream_t));
			return buffer;
		}

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

		public static void LZ4_setStreamDecode(
			LZ4_streamDecode_t* LZ4_streamDecode, byte* dictionary, int dictSize)
		{
			var lz4sd = LZ4_streamDecode;
			lz4sd->prefixSize = (uint) dictSize;
			lz4sd->prefixEnd = dictionary + dictSize;
			lz4sd->externalDict = null;
			lz4sd->extDictSize = 0;
		}

		protected static readonly uint[] inc32table = { 0, 1, 2, 1, 0, 4, 4, 4 };
		protected static readonly int[] dec64table = { 0, 0, 0, -1, -4, 1, 2, 3 };
	}
}
