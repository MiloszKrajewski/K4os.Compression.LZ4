using System;
using System.IO;
using BenchmarkDotNet.Attributes;
using K4os.Compression.LZ4.Internal;
using TestHelpers;
using LZ4PrevCodec = K4os.Compression.LZ4.vPrev.LZ4Codec;

namespace K4os.Compression.LZ4.Benchmarks
{
	public class FastBlockCompression
	{
		private byte[] _source;
		private byte[] _target;

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
