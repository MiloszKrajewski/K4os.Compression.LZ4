using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Xunit;
using _LZ4 = LZ4.LZ4Codec;

namespace K4os.Compression.LZ4.Test
{

	public class BlockRoundtripTests
	{
		const string Lorem =
			"Lorem ipsum dolor sit amet, consectetur adipiscing elit, " +
			"sed do eiusmod tempor incididunt ut labore et dolore magna aliqua. " +
			"Ut enim ad minim veniam, quis nostrud exercitation ullamco laboris " +
			"nisi ut aliquip ex ea commodo consequat. Duis aute irure dolor in " +
			"reprehenderit in voluptate velit esse cillum dolore eu fugiat nulla pariatur. " +
			"Excepteur sint occaecat cupidatat non proident, " +
			"sunt in culpa qui officia deserunt mollit anim id est laborum.";

		private static byte[] Collect(IEnumerable<byte[]> chunks)
		{
			var arrays = chunks.ToArray();
			var length = arrays.Select(x => x.Length).Sum();
			var result = new byte[length];
			var index = 0;
			foreach (var c in arrays)
			{
				Array.Copy(c, 0, result, index, c.Length);
				index += c.Length;
			}

			return result;
		}

		private static void Roundtrip(byte[] source)
		{
			var compressedOld = global::LZ4.LZ4Codec.Encode(source, 0, source.Length);
			var compressedNew = LZ4Codec.Encode(source, 0, source.Length, LZ4Level.L00_FAST);

			Tools.SameBytes(
				source,
				_LZ4.Decode(compressedNew, 0, compressedNew.Length, source.Length));

			Tools.SameBytes(
				source,
				LZ4Codec.Decode(compressedNew, 0, compressedNew.Length, source.Length));

			Tools.SameBytes(
				source,
				LZ4Codec.Decode(compressedOld, 0, compressedOld.Length, source.Length));
		}

		[Theory]
		[InlineData(160)]
		[InlineData(0)]
		[InlineData(255)]
		[InlineData(65)]
		public void SingleByteRountrip(byte value)
		{
			Roundtrip(new[] { value });
		}

		[Theory]
		[InlineData(160, 33)]
		[InlineData(0, 13)]
		[InlineData(0, 15)]
		[InlineData(0, 17)]
		[InlineData(255, 1000)]
		[InlineData(65, 67)]
		public void RepeatedByteRountrip(byte value, int length)
		{
			var bytes = new byte[length];
			for (var i = 0; i < length; i++) bytes[i] = value;

			Roundtrip(bytes);
		}

		[Theory]
		[InlineData(1)]
		[InlineData(1000)]
		[InlineData(0x7FFF)]
		[InlineData(0xFFFF)]
		[InlineData(0x123456)]
		public void RepeatLoremIpsum(int length)
		{
			var loremBytes = Encoding.UTF8.GetBytes(Lorem);
			var buffer = Collect(Enumerable.Repeat(loremBytes, length / Lorem.Length + 1));

			Roundtrip(buffer);
		}

		[Theory]
		[InlineData(0, 1000)]
		[InlineData(1, 0x7FFF)]
		[InlineData(2, 0xFFFF)]
		[InlineData(3, 0xFFFF)]
		[InlineData(4, 0x123456)]
		public void UncompressibleData(int seed, int length)
		{
			var buffer = new byte[length];
			new Random(seed).NextBytes(buffer);

			Roundtrip(buffer);
		}
	}
}
