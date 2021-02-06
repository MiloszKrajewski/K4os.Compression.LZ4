#if NET5_0

using System;
using System.Numerics;
using System.Runtime.CompilerServices;
using Xunit;

namespace K4os.Compression.LZ4.Test
{
	public class HardwareCtzTests
	{
		[Fact]
		public void AmITestingTheRightThing()
		{
			Assert.Equal(0u, HW_CTZ_32(0x01));
			Assert.Equal(0u, HW_CTZ_32(0x80));
			Assert.Equal(1u, HW_CTZ_32(0x0100));
			Assert.Equal(2u, HW_CTZ_32(0x010000));
			Assert.Equal(3u, HW_CTZ_32(0x06000000));
			Assert.Equal(0u, HW_CTZ_32(0));
			
			Assert.Equal(0u, SW_CTZ_32(0x01));
			Assert.Equal(0u, SW_CTZ_32(0x80));
			Assert.Equal(1u, SW_CTZ_32(0x0100));
			Assert.Equal(2u, SW_CTZ_32(0x010000));
			Assert.Equal(3u, SW_CTZ_32(0x06000000));
			Assert.Equal(0u, SW_CTZ_32(0));
		}

		[Fact]
		public void SoftwareAndHardwareCtz32Match()
		{
			Assert.Equal(SW_CTZ_32(0), HW_CTZ_32(0));

			var rand = new Random(0);
			for (var i = 0; i <= 32; i++)
			{
				var value = (uint) (rand.Next() | 0x01) << i;
				Assert.Equal(SW_CTZ_32(value), HW_CTZ_32(value));
			}
		}

		[Fact]
		public void SoftwareAndHardwareCtz64Match()
		{
			Assert.Equal(SW_CTZ_64(0), HW_CTZ_64(0));

			var rand = new Random(0);
			for (var i = 0; i <= 64; i++)
			{
				var value = (ulong) (rand.Next() | 0x01) << i;
				Assert.Equal(SW_CTZ_64(value), HW_CTZ_64(value));
			}
		}
		
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
		
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static uint HW_CTZ_32(uint val) =>
			((uint) BitOperations.TrailingZeroCount(val) >> 3) & 0x03;
		
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

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static uint HW_CTZ_64(ulong val) =>
			((uint) BitOperations.TrailingZeroCount(val) >> 3) & 0x07;
	}
}

#endif

