using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using Xunit;
using Xunit.Abstractions;

namespace K4os.Compression.LZ4.Test
{
	public class PerformancePlayground
	{
		private ITestOutputHelper _output;

		public PerformancePlayground(ITestOutputHelper output) => _output = output;

		private void WriteLine(string text) => _output.WriteLine(text);

		private void Measure(string name, int count, Action action)
		{
			action();
			Thread.Sleep(200);
			var stopwatch = Stopwatch.StartNew();
			for (var i = 0; i < count; i++)
				action();
			stopwatch.Stop();
			WriteLine($"{name}: {stopwatch.Elapsed.TotalMilliseconds / count:0.0000}ms");
		}

		[Fact]
		public void MeasureDickens()
		{
			var input = File.ReadAllBytes("D:/Projects/K4os.Compression.LZ4/.corpus/dickens");
			var output = new byte[LZ4Interface.MaximumOutputSize(input.Length)];
			Measure("compress", 100, () => LZ4Interface.Encode(input, output));
		}
	}
}
