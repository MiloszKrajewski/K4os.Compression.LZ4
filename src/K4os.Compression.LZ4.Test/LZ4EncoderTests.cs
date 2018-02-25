using System;
using K4os.Compression.LZ4.Encoders;
using Xunit;
using Xunit.Abstractions;

namespace K4os.Compression.LZ4.Test
{
	public class LZ4EncoderTests
	{
		private readonly ITestOutputHelper _output;
		public LZ4EncoderTests(ITestOutputHelper output) => _output = output;
		protected void WriteLine(string text) => _output.WriteLine(text);

		[Fact]
		public void FastEncoderEncodesAllBytes()
		{
			var size = 1100;
			var input = new byte[size];
			var output = new byte[LZ4Codec.MaximumOutputSize(size)];
			Lorem.Fill(input, 0, input.Length);
			var encoder = new LZ4FastStreamEncoder(1024);
			var inputIndex = 0;
			var outputIndex = 0;

			while (inputIndex < input.Length)
			{
				var chunkLength = Math.Min(1024, input.Length - inputIndex);
				var tailLength = output.Length - outputIndex;
				var success = encoder.TopupAndEncode(
					input,
					inputIndex,
					chunkLength,
					output,
					outputIndex,
					tailLength,
					false,
					out var loaded,
					out var encoded);

				inputIndex += loaded;
				outputIndex += encoded;

				if (!success)
					throw new InvalidOperationException();
			}

			outputIndex += encoder.Encode(output, outputIndex, output.Length - outputIndex);

			WriteLine($"output: {outputIndex}");
		}
	}
}
