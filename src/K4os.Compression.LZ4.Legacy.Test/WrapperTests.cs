using System;
using System.Linq;
using K4os.Compression.LZ4.Internal;
using TestHelpers;
using Xunit;
using ReferenceLZ4 = LZ4.LZ4Codec;

namespace K4os.Compression.LZ4.Legacy.Test
{
	public class WrapperTests
	{
        private static void ParseSettings(
			string settings, out bool high, out bool referenceEncoder, out bool referenceDecoder)
		{
			high = settings.Contains('h');
			referenceEncoder = settings.Contains('e');
			referenceDecoder = settings.Contains('d');
		}

		[Theory]
		[InlineData(0, "")]
		[InlineData(0, "h")]
		[InlineData(0, "he")]
		[InlineData(0, "hd")]
		public void ZeroLengthArrayIsWorking(int seed, string settings)
		{
			ParseSettings(
				settings, 
				out var high, out var referenceEncoder, out var referenceDecoder);
			Roundtrip(seed, new byte[0], high, referenceEncoder, referenceDecoder);
		}

		[Theory]
		[InlineData(0, 1337, "")]
		[InlineData(0, 1337, "h")]
		[InlineData(0, 1337, "he")]
		[InlineData(0, 1337, "hd")]
		[InlineData(0, Mem.M4, "")]
		[InlineData(0, Mem.M1, "h")]
		[InlineData(-1, Mem.M4, "")]
		[InlineData(1337, Mem.M1, "h")]
		public void CompressibleDataIsCompressed(
			int seed, int length, string settings)
		{
			ParseSettings(
				settings, 
				out var high, out var referenceEncoder, out var referenceDecoder);

			var buffer = new byte[length];
			Lorem.Fill(buffer, 0, length);

			Roundtrip(seed, buffer, high, referenceEncoder, referenceDecoder);
		}

		[Theory]
		[InlineData(0, 1337, "")]
		[InlineData(0, 1337, "h")]
		[InlineData(0, 1337, "he")]
		[InlineData(0, 1337, "hd")]
		[InlineData(0, Mem.M4, "")]
		[InlineData(0, Mem.M1, "h")]
		public void HighEntropyDataIsCopiedAsIs(
			int seed, int length, string settings)
		{
			ParseSettings(
				settings, 
				out var high, out var referenceEncoder, out var referenceDecoder);

			var buffer = new byte[length];
			new Random(seed).NextBytes(buffer);

			Roundtrip(seed, buffer, high, referenceEncoder, referenceDecoder);
		}

		public byte[] Wrap(byte[] buffer, int head, int tail, bool high, bool reference)
		{
			var length = buffer.Length;
			var input = new byte[length + head + tail];
			for (var i = 0; i < input.Length; i++) input[i] = 0xCD;
			Buffer.BlockCopy(buffer, 0, input, head, length);

			return reference
				? high
					? ReferenceLZ4.WrapHC(input, head, length)
					: ReferenceLZ4.Wrap(input, head, length)
				: high
					? LZ4Legacy.WrapHC(input, head, length)
					: LZ4Legacy.Wrap(input, head, length);
		}

		public byte[] Unwrap(byte[] buffer, int head, int tail, bool reference)
		{
			var length = buffer.Length;
			var input = new byte[length + head + tail];
			for (var i = 0; i < input.Length; i++) input[i] = 0xCD;
			Buffer.BlockCopy(buffer, 0, input, head, length);

			return reference
				? ReferenceLZ4.Unwrap(input, head)
				: LZ4Legacy.Unwrap(input, head);
		}

		private void Roundtrip(
			int seed, byte[] original, bool high, bool referenceEncoder, bool referenceDecoder)
		{
			var random = new Random(seed);
			int Next() => seed < 0 ? 0 : random.Next(64);
			var encoded = Wrap(original, Next(), Next(), high, referenceEncoder);
			var decoded = Unwrap(encoded, Next(), Next(), referenceDecoder);
			Assert.Equal(original, decoded);
		}
	}
}
