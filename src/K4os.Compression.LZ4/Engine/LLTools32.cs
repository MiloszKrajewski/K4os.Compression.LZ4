//---------------------------------------------------------
//
// This file has been generated. All changes will be lost.
//
//---------------------------------------------------------
#define BIT32

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
	internal unsafe class LLTools32: LLTools
	#else
	internal unsafe class LLTools64: LLTools
	#endif
	{
		#if BIT32
		protected const bool BIT32 = true;
		protected const int ARCH_SIZE = 4;

		private static readonly uint[] DeBruijnBytePos = {
			0, 0, 3, 0, 3, 1, 3, 0,
			3, 2, 2, 1, 3, 2, 0, 1,
			3, 3, 1, 2, 2, 2, 2, 0,
			3, 1, 2, 0, 1, 0, 1, 1
		};

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		protected static uint LZ4_NbCommonBytes(uint val) =>
			DeBruijnBytePos[
				unchecked((uint) ((int) val & -(int) val) * 0x077CB531U >> 27)];
		#else
		protected const bool BIT32 = false;
		protected const int ARCH_SIZE = 8;

		private static readonly uint[] DeBruijnBytePos = {
			0, 0, 0, 0, 0, 1, 1, 2,
			0, 3, 1, 3, 1, 4, 2, 7,
			0, 2, 3, 6, 1, 5, 3, 5,
			1, 3, 4, 4, 2, 5, 6, 7,
			7, 0, 1, 2, 3, 3, 4, 6,
			2, 6, 5, 5, 3, 4, 5, 6,
			7, 1, 2, 4, 6, 4, 4, 5,
			7, 2, 6, 5, 7, 6, 7, 7
		};

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		protected static uint LZ4_NbCommonBytes(ulong val) =>
			DeBruijnBytePos[
				unchecked((ulong) ((long) val & -(long) val) * 0x0218A392CDABBD3Ful >> 58)];
		#endif

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		protected static uint LZ4_count(byte* pIn, byte* pMatch, byte* pInLimit)
		{
			const int STEPSIZE = ARCH_SIZE;

			var pStart = pIn;

			if (pIn < pInLimit - (STEPSIZE - 1))
			{
				var diff = Mem.PeekW(pMatch) ^ Mem.PeekW(pIn);
				if (diff != 0)
					return LZ4_NbCommonBytes(diff);

				pIn += STEPSIZE;
				pMatch += STEPSIZE;
			}

			while (pIn < pInLimit - (STEPSIZE - 1))
			{
				var diff = Mem.PeekW(pMatch) ^ Mem.PeekW(pIn);
				if (diff != 0)
					return (uint) (pIn + LZ4_NbCommonBytes(diff) - pStart);

				pIn += STEPSIZE;
				pMatch += STEPSIZE;
			}

			#if !BIT32

			if (pIn < pInLimit - 3 && Mem.Peek4(pMatch) == Mem.Peek4(pIn))
			{
				pIn += 4;
				pMatch += 4;
			}

			#endif

			if (pIn < pInLimit - 1 && Mem.Peek2(pMatch) == Mem.Peek2(pIn))
			{
				pIn += 2;
				pMatch += 2;
			}

			if (pIn < pInLimit && *pMatch == *pIn)
				pIn++;

			return (uint) (pIn - pStart);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		protected static uint LZ4_hashPosition(void* p, tableType_t tableType)
		{
			#if !BIT32
			if (tableType != tableType_t.byU16)
				return LZ4_hash5(Mem.PeekW(p), tableType);
			#endif
			return LZ4_hash4(Mem.Peek4(p), tableType);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		protected static void LZ4_putPosition(
			byte* p, void* tableBase, tableType_t tableType, byte* srcBase) =>
			LZ4_putPositionOnHash(p, LZ4_hashPosition(p, tableType), tableBase, tableType, srcBase);

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		protected static byte* LZ4_getPosition(
			byte* p, void* tableBase, tableType_t tableType, byte* srcBase) =>
			LZ4_getPositionOnHash(LZ4_hashPosition(p, tableType), tableBase, tableType, srcBase);

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static LZ4_stream_t* LZ4_createStream() =>
			LZ4_initStream((LZ4_stream_t*) Mem.Alloc(sizeof(LZ4_stream_t)));

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static LZ4_stream_t* LZ4_initStream(LZ4_stream_t* buffer)
		{
			Mem.Zero((byte*) buffer, sizeof(LZ4_stream_t));
			return buffer;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void LZ4_freeStream(LZ4_stream_t* LZ4_stream)
		{
			if (LZ4_stream != null) Mem.Free(LZ4_stream);
		}

		protected static void LZ4_renormDictT(LZ4_stream_t* LZ4_dict, int nextSize)
		{
			Debug.Assert(nextSize >= 0);
			if (LZ4_dict->currentOffset + (uint) nextSize <= 0x80000000) return;

			var delta = LZ4_dict->currentOffset - 64 * KB;
			var dictEnd = LZ4_dict->dictionary + LZ4_dict->dictSize;
			for (var i = 0; i < LZ4_HASH_SIZE_U32; i++)
			{
				if (LZ4_dict->hashTable[i] < delta) LZ4_dict->hashTable[i] = 0;
				else LZ4_dict->hashTable[i] -= delta;
			}

			LZ4_dict->currentOffset = 64 * KB;
			if (LZ4_dict->dictSize > 64 * KB) LZ4_dict->dictSize = 64 * KB;
			LZ4_dict->dictionary = dictEnd - LZ4_dict->dictSize;
		}

		public int LZ4_loadDict(LZ4_stream_t* LZ4_dict, byte* dictionary, int dictSize)
		{
			const int HASH_UNIT = ARCH_SIZE;
			var dict = LZ4_dict;
			const tableType_t tableType = tableType_t.byU32;
			var p = dictionary;
			var dictEnd = p + dictSize;

			LZ4_initStream(LZ4_dict);

			dict->currentOffset += 64 * KB;

			if (dictSize < HASH_UNIT)
			{
				return 0;
			}

			if (dictEnd - p > 64 * KB) p = dictEnd - 64 * KB;
			var @base = dictEnd - dict->currentOffset;

			dict->dictionary = p;
			dict->dictSize = (uint) (dictEnd - p);
			dict->tableType = tableType;

			while (p <= dictEnd - HASH_UNIT)
			{
				LZ4_putPosition(p, dict->hashTable, tableType, @base);
				p += 3;
			}

			return (int) dict->dictSize;
		}

		public int LZ4_saveDict(LZ4_stream_t* LZ4_dict, byte* safeBuffer, int dictSize)
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
	}
}

