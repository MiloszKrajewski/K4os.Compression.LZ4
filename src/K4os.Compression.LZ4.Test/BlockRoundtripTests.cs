using System;
using System.Linq;
using Xunit;
using _LZ4 = LZ4.LZ4Codec;

namespace K4os.Compression.LZ4.Test
{
	public class BlockRoundtripTests
	{
		private static void Roundtrip(byte[] source)
		{
			var compressedOld = _LZ4.Encode(source, 0, source.Length);
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
			var buffer = new byte[length];
			Lorem.Fill(buffer, 0, length);

			Roundtrip(buffer);
		}

		[Theory]
		[InlineData(0, 1000)]
		[InlineData(1, 0x7FFF)]
		[InlineData(2, 0xFFFF)]
		[InlineData(3, 0xFFFF)]
		[InlineData(4, 0x123456)]
		public void IncompressibleData(int seed, int length)
		{
			var buffer = new byte[length];
			new Random(seed).NextBytes(buffer);

			Roundtrip(buffer);
		}

		[Fact]
		public void BorderLineCompressions()
		{
			var original = Tools.LoadChunk(Tools.FindFile(".corpus/x-ray"), 0, 0x10000);
			var target = new byte[LZ4Codec.MaximumOutputSize(original.Length)];
			var required = LZ4Codec.Encode(original, 0, original.Length, target, 0, target.Length, LZ4Level.L00_FAST);
			var minimal = LZ4Codec.Encode(original, 0, original.Length, target, 0, required, LZ4Level.L00_FAST);
			Assert.Equal(required, minimal);
		}
	}
}
