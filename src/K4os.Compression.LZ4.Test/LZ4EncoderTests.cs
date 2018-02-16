using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace K4os.Compression.LZ4.Test
{
	public class LZ4EncoderTests
	{
		[Fact]
		public void xxx()
		{
			const int chunkSize = 1023;
			const int blockSize = 1024;
			var input = new byte[chunkSize * blockSize];
			var output = new byte[chunkSize * blockSize];

			new Random(0).NextBytes(input);
			new Random(1).NextBytes(output); // different then input

			var encoder = new LZ4Encoder(blockSize);
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