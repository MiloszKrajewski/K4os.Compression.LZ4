// ReSharper disable IdentifierTypo
// ReSharper disable InconsistentNaming
// ReSharper disable AccessToStaticMemberViaDerivedType
// ReSharper disable ConditionIsAlwaysTrueOrFalse
// ReSharper disable BuiltInTypeReferenceStyle

using System;
using System.Runtime.CompilerServices;

#if BIT32
using reg_t = System.UInt32;
using Mem = K4os.Compression.LZ4.Internal.Mem32;
#else
using reg_t = System.UInt64;
using Mem = K4os.Compression.LZ4.Internal.Mem64;
#endif

#if NET5_0_OR_GREATER && !BIT32
using System.Numerics;
#endif

using size_t = System.UInt32;
using uptr_t = System.UInt64;

namespace K4os.Compression.LZ4.Engine;

#if BIT32
internal unsafe partial class LL32: LL
#else
internal unsafe partial class LL64: LL
#endif
{
	#if BIT32
	
	protected const int ALGORITHM_ARCH = 4;

	private static readonly uint[] _DeBruijnBytePos = {
		0, 0, 3, 0, 3, 1, 3, 0,
		3, 2, 2, 1, 3, 2, 0, 1,
		3, 3, 1, 2, 2, 2, 2, 0,
		3, 1, 2, 0, 1, 0, 1, 1,
	};

	private static readonly uint* DeBruijnBytePos = Mem.CloneArray(_DeBruijnBytePos);

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	protected static uint LZ4_NbCommonBytes(uint val) =>
		// ReSharper disable once RedundantCast
		DeBruijnBytePos[(uint)unchecked(((uint)((int)val & -(int)val) * 0x077CB531u) >> 27)];

	#else // BIT32

	protected const int ALGORITHM_ARCH = 8;

	#if NET5_0_OR_GREATER
	
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	protected static uint LZ4_NbCommonBytes(ulong val) =>
		((uint) BitOperations.TrailingZeroCount(val) >> 3) & 0x07;

	#else // NET5_0_OR_GREATER

	private static readonly uint[] _DeBruijnBytePos = {
		0, 0, 0, 0, 0, 1, 1, 2,
		0, 3, 1, 3, 1, 4, 2, 7,
		0, 2, 3, 6, 1, 5, 3, 5,
		1, 3, 4, 4, 2, 5, 6, 7,
		7, 0, 1, 2, 3, 3, 4, 6,
		2, 6, 5, 5, 3, 4, 5, 6,
		7, 1, 2, 4, 6, 4, 4, 5,
		7, 2, 6, 5, 7, 6, 7, 7,
	};
	
	private static readonly uint* DeBruijnBytePos = Mem.CloneArray(_DeBruijnBytePos);

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	protected static uint LZ4_NbCommonBytes(ulong val) =>
		DeBruijnBytePos[
			(uint)unchecked(((ulong)((long)val & -(long)val) * 0x0218A392CDABBD3Ful) >> 58)
		];

	#endif // NET5_0_OR_GREATER

	#endif // BIT32

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	protected static uint LZ4_count(byte* pIn, byte* pMatch, byte* pInLimit)
	{
		const int STEPSIZE = ALGORITHM_ARCH;

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
				return (uint)(pIn + LZ4_NbCommonBytes(diff) - pStart);

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

		return (uint)(pIn - pStart);
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

	#region dictionary

	protected static void LZ4_renormDictT(LZ4_stream_t* LZ4_dict, int nextSize)
	{
		Assert(nextSize >= 0);
		if (LZ4_dict->currentOffset + (uint)nextSize <= 0x80000000) return;

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
		const int HASH_UNIT = ALGORITHM_ARCH;
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
		dict->dictSize = (uint)(dictEnd - p);
		dict->tableType = tableType;

		while (p <= dictEnd - HASH_UNIT)
		{
			LZ4_putPosition(p, dict->hashTable, tableType, @base);
			p += 3;
		}

		return (int)dict->dictSize;
	}

	#endregion
}

