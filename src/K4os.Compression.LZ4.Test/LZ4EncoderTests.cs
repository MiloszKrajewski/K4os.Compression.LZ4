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
			var encoder = new LZ4FastStreamEncoder(1024, 64);
			var inputIndex = 0;
			var outputIndex = 0;

			while (inputIndex < input.Length)
			{
				var chunkLength = Math.Min(1024, input.Length - inputIndex);
				var loaded = encoder.Topup(input, inputIndex, chunkLength);
				inputIndex += loaded;
				if (loaded == 0)
					outputIndex += encoder.Encode(output, outputIndex, output.Length - outputIndex);
			}

			outputIndex += encoder.Encode(output, outputIndex, output.Length - outputIndex);

			Console.WriteLine($"output: {outputIndex}");
		}
	}
}
