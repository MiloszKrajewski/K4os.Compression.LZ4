using System;
using System.Numerics;
using System.Runtime.CompilerServices;

namespace Benchmarks
{
	public class BitOps
	{
		private static readonly uint[] DeBruijnBytePos32 = {
			0, 0, 3, 0, 3, 1, 3, 0,
			3, 2, 2, 1, 3, 2, 0, 1,
			3, 3, 1, 2, 2, 2, 2, 0,
			3, 1, 2, 0, 1, 0, 1, 1,
		};

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static uint SW_CTZ_32(uint val) =>
			DeBruijnBytePos32[
				unchecked((uint) ((int) val & -(int) val) * 0x077CB531U >> 27)];
		
		private static readonly uint[] DeBruijnBytePos64 = {
			0, 0, 0, 0, 0, 1, 1, 2,
			0, 3, 1, 3, 1, 4, 2, 7,
			0, 2, 3, 6, 1, 5, 3, 5,
			1, 3, 4, 4, 2, 5, 6, 7,
			7, 0, 1, 2, 3, 3, 4, 6,
			2, 6, 5, 5, 3, 4, 5, 6,
			7, 1, 2, 4, 6, 4, 4, 5,
			7, 2, 6, 5, 7, 6, 7, 7,
		};

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static uint SW_CTZ_64(ulong val) =>
			DeBruijnBytePos64[
				unchecked((ulong) ((long) val & -(long) val) * 0x0218A392CDABBD3Ful >> 58)];
		
		#if NETCOREAPP3_1 || NET5_0_OR_GREATER
		
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static uint HW_CTZ_32(uint val) =>
			((uint) BitOperations.TrailingZeroCount(val) >> 3) & 0x03;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static uint HW_CTZ_64(ulong val) =>
			((uint) BitOperations.TrailingZeroCount(val) >> 3) & 0x07;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static uint HW_CTZ_64_x(ulong val) =>
			(uint) BitOperations.TrailingZeroCount(val) >> 3;

		#endif
	}
}
