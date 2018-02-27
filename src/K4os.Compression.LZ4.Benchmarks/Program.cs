using BenchmarkDotNet.Running;

namespace K4os.Compression.LZ4.Benchmarks
{
	class Program
	{
		static void Main(string[] args)
		{
			BenchmarkRunner.Run<CompareMemCopy>();
		}
	}
}
