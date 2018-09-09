using System;
using System.Drawing.Drawing2D;
using K4os.Compression.LZ4.Internal;
using Xunit;

namespace K4os.Compression.LZ4.Test
{
	public class SpanTests
	{
		[Theory]
		[InlineData(0, 0)]
		[InlineData(0, 32)]
		[InlineData(0, 1337)]
		[InlineData(16, 1337)]
		[InlineData(1337, 1337)]
		[InlineData(1337, 1)]
		[InlineData(Mem.K64, Mem.K64)]
		public void CanEncodePartOfBuffer(int offset, int sourceLength)
		{
			var sourceTotal = offset + sourceLength + offset;
			var targetLength = LZ4Codec.MaximumOutputSize(sourceTotal);
			var targetTotal = offset + targetLength + offset;
			var source = new byte[sourceTotal];
			var encoded = new byte[targetTotal];
			var decoded = new byte[sourceTotal];

			Lorem.Fill(source, 0, source.Length);
			Fill(encoded, 0xCD);
			Fill(decoded, 0xCD);

			var encodedLength = LZ4Codec.Encode(
				source, offset, sourceLength, encoded, offset, targetLength);

			Check(encoded, 0, offset, 0xCD);
			Check(encoded, offset + encodedLength, offset, 0xCD);

			var decodedLength = LZ4Codec.Decode(
				encoded, offset, encodedLength, decoded, offset, sourceLength);

			Assert.Equal(sourceLength, decodedLength);
			Check(decoded, 0, offset, 0xCD);
			Check(decoded, offset + decodedLength, offset, 0xCD);
			Check(decoded, offset, decodedLength, source);
		}

		[Theory]
		[InlineData(0, 0)]
		[InlineData(0, 32)]
		[InlineData(0, 1337)]
		[InlineData(16, 1337)]
		[InlineData(1337, 1337)]
		[InlineData(1337, 1)]
		[InlineData(Mem.K64, Mem.K64)]
		public void CanEncodePartOfBufferWithSpan(int offset, int sourceLength)
		{
			var sourceTotal = offset + sourceLength + offset;
			var targetLength = LZ4Codec.MaximumOutputSize(sourceTotal);
			var targetTotal = offset + targetLength + offset;
			var source = new byte[sourceTotal];
			var encoded = new byte[targetTotal];
			var decoded = new byte[sourceTotal];

			Lorem.Fill(source, 0, source.Length);
			Fill(encoded, 0xCD);
			Fill(decoded, 0xCD);

			var encodedLength = LZ4Codec.Encode(
				source.AsSpan(offset, sourceLength),
				encoded.AsSpan(offset, targetLength));

			Check(encoded, 0, offset, 0xCD);
			Check(encoded, offset + encodedLength, offset, 0xCD);

			var decodedLength = LZ4Codec.Decode(
				encoded.AsSpan(offset, encodedLength),
				decoded.AsSpan(offset, sourceLength));

			Assert.Equal(sourceLength, decodedLength);
			Check(decoded, 0, offset, 0xCD);
			Check(decoded, offset + decodedLength, offset, 0xCD);
			Check(decoded, offset, decodedLength, source);
		}

		private static void Fill(byte[] buffer, byte value)
		{
			for (var i = 0; i < buffer.Length; i++)
				buffer[i] = value;
		}

		private void Check(byte[] buffer, int offset, int length, byte value)
		{
			for (var i = offset; i < offset + length; i++)
				Assert.True(buffer[i] == value, $"Value overriden @ {i}");
		}

		private void Check(byte[] buffer, int offset, int length, byte[] source)
		{
			for (var i = offset; i < offset + length; i++)
				Assert.True(buffer[i] == source[i], $"Value different @ {i}");
		}
	}
}
