using System;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using K4os.Compression.LZ4.Internal;
using K4os.Compression.LZ4.Tests.Adapters;
using TestHelpers;
using Xunit;

namespace K4os.Compression.LZ4.Tests
{
	public class BlockRoundtripTests
	{
		private static void Roundtrip(byte[] source, bool enforce32)
		{
			LZ4Codec.Enforce32 = enforce32;
			try
			{
				var legacy = LegacyLZ4.Encode(source, 0, source.Length);
				var baseline = BaselineLZ4.Encode(source, 0, source.Length);
				var current = CurrentLZ4.Encode(source, 0, source.Length, LZ4Level.L00_FAST);

				void TestDecode(byte[] compressed, Func<byte[], int, int, int, byte[]> func) =>
					Tools.SameBytes(source, func(compressed, 0, compressed.Length, source.Length));

				TestDecode(current, LegacyLZ4.Decode);
				TestDecode(current, BaselineLZ4.Decode);
				TestDecode(current, CurrentLZ4.Decode);

				TestDecode(legacy, CurrentLZ4.Decode);
				TestDecode(baseline, CurrentLZ4.Decode);
			}
			finally
			{
				LZ4Codec.Enforce32 = false;
			}
		}

		private static void Roundtrip(byte[] source)
		{
			Roundtrip(source, false);
			if (!Mem.System32) Roundtrip(source, true);
		}

		[Fact]
		public void QuickFoxRoundtrip()
		{
			var text = "The quick brown fox jumps over the lazy dog";
			var textBytes = Encoding.UTF8.GetBytes(text);

			var encoded = new byte[LZ4Codec.MaximumOutputSize(textBytes.Length)];
			var encodedLength = LZ4Codec.Encode(
				textBytes, 0, textBytes.Length,
				encoded, 0, encoded.Length);

			var decoded = new byte[textBytes.Length * 2];
			var decodedLength = LZ4Codec.Decode(
				encoded, 0, encodedLength,
				decoded, 0, decoded.Length);

			Assert.Equal(textBytes.Length, decodedLength); // -1 instead of 43
		}

		[Theory]
		[InlineData(160)]
		[InlineData(0)]
		[InlineData(255)]
		[InlineData(65)]
		public void SingleByteRountrip(byte value) { Roundtrip(new[] { value }); }

		[Theory]
		[InlineData(160, 33)]
		[InlineData(0, 13)]
		[InlineData(0, 15)]
		[InlineData(0, 17)]
		[InlineData(255, 1000)]
		[InlineData(65, 67)]
		[InlineData(0xAA, Mem.K64)]
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
		[SuppressMessage("ReSharper", "RedundantArgumentDefaultValue")]
		public void BorderLineCompressions()
		{
			var original = Tools.LoadChunk(Tools.FindFile(".corpus/x-ray"), 0, 0x10000);
			var target = new byte[LZ4Codec.MaximumOutputSize(original.Length)];
			var required = LZ4Codec.Encode(
				original, 0, original.Length, target, 0, target.Length, LZ4Level.L00_FAST);
			var minimal = LZ4Codec.Encode(
				original, 0, original.Length, target, 0, required, LZ4Level.L00_FAST);
			Assert.Equal(required, minimal);
		}
	}
}
