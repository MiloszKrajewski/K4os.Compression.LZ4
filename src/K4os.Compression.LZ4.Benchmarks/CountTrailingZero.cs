using System;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;

namespace K4os.Compression.LZ4.Benchmarks
{
	// [SimpleJob(RuntimeMoniker.NetCoreApp31)]
	[SimpleJob(RuntimeMoniker.NetCoreApp50)]
	[DisassemblyDiagnoser]
	public class CountTrailingZero
	{
		private uint _input32;
		private ulong _input64;
		private uint _output32;
		private ulong _output64;


		[GlobalSetup]
		public void Setup()
		{
			_input32 = 0x394328 << 21;
			_input64 = 0x394328ul << 43;
		}

		[Benchmark]
		public void Software32()
		{
			_output32 = BitOps.SW_CTZ_32(_input32);
		}
		
		[Benchmark]
		public void Software64()
		{
			_output64 = BitOps.SW_CTZ_64(_input64);
		}
		
		#if NETCOREAPP3_1 || NET5_0
		
		[Benchmark]
		public void Hardware32()
		{
			_output32 = BitOps.HW_CTZ_32(_input32);
		}
		
		[Benchmark]
		public void Hardware64()
		{
			_output64 = BitOps.HW_CTZ_64(_input64);
		}
		
		[Benchmark]
		public void Hardware64X()
		{
			_output64 = BitOps.HW_CTZ_64_x(_input64);
		}
		
		#endif
	}
}
