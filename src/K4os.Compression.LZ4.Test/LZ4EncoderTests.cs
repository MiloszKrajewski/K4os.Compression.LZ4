using System;
using K4os.Compression.LZ4.Encoders;
using Xunit;

namespace K4os.Compression.LZ4.Test
{
	public class LZ4EncoderTests
	{
		[Fact]
		public void FastEncoderEncodesAllBytes()
		{
			var input = new byte[16 * 1024 * 1024];
			var output = new byte[16 * 1024 * 1024];
			Lorem.Fill(input, 0, input.Length);
			var encoder = new LZ4FastEncoder(1024);
			var inputIndex = 0;
			var outputIndex = 0;

			while (inputIndex < input.Length)
			{
				var chunkLength = Math.Min(1024, input.Length - inputIndex);
				var encoded = encoder.Encode(input, inputIndex, chunkLength, output, outputIndex, output.Length - outputIndex);
				inputIndex += chunkLength;
				outputIndex += encoded;
			}
		}

		[Fact]
		public void NoopEncoderEncodesAllBytes()
		{
			const int chunkSize = 1023;
			const int blockSize = 1024;
			var input = new byte[chunkSize * blockSize];
			var output = new byte[chunkSize * blockSize];

			new Random(0).NextBytes(input);
			new Random(1).NextBytes(output); // different than input

			var encoder = new LZ4NoopEncoder(blockSize);
			var inputIndex = 0;
			var outputIndex = 0;

			while (inputIndex < input.Length)
			{
				var chunkLength = Math.Min(chunkSize, input.Length - inputIndex);
				var blockLength = Math.Min(blockSize, output.Length - outputIndex);
				var encoded = encoder.Encode(input, inputIndex, chunkLength, output, outputIndex, blockLength);
				inputIndex += chunkLength;
				outputIndex += encoded;
			}

			for (var i = 0; i < input.Length; i++)
				Assert.Equal(input[i], output[i]);
		}
	}
}
