using System;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using K4os.Compression.LZ4;
using TestHelpers;
using LZ4PrevCodec = K4os.Compression.LZ4.vPrev.LZ4Codec;

namespace Benchmarks
{
	[SimpleJob(RuntimeMoniker.NetCoreApp31)]
	[SimpleJob(RuntimeMoniker.Net60)]
	public class FastBlockCompression
	{
		private byte[] _source = null!;
		private byte[] _target = null!;

		[GlobalSetup]
		public void Setup()
		{
			var filename = Tools.FindFile(".corpus/xml");
			_source = File.ReadAllBytes(filename);
			_target = new byte[LZ4Codec.MaximumOutputSize(_source.Length)];
		}

		[Benchmark]
		public void Previous64()
		{
			LZ4PrevCodec.Encode(
				_source, 0, _source.Length,
				_target, 0, _target.Length);
		}

		[Benchmark]
		public void Current64()
		{
			LZ4Codec.Encode(
				_source, 0, _source.Length,
				_target, 0, _target.Length);
		}

		[Benchmark]
		public void Current32()
		{
			LZ4Codec.Enforce32 = true;
			LZ4Codec.Encode(
				_source, 0, _source.Length,
				_target, 0, _target.Length);
			LZ4Codec.Enforce32 = false;
		}
	}
}
