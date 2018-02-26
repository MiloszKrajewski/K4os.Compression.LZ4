using System;
using System.IO;
using Xunit;
using Xunit.Abstractions;

namespace K4os.Compression.LZ4.Test
{
	public class PerformancePlayground: TestBase
	{
		public PerformancePlayground(ITestOutputHelper output): base(output) { }

		[Fact]
		public void MeasureDickens()
		{
			var input = File.ReadAllBytes(Tools.FindFile(".corpus/dickens"));
			var output = new byte[LZ4Codec.MaximumOutputSize(input.Length)];
			Measure(
				"compress",
				100,
				() => LZ4Codec.Encode(input, 0, input.Length, output, 0, output.Length, LZ4Level.L00_FAST));
		}
	}
}
