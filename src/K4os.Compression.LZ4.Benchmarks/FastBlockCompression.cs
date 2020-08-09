using System.IO;
using BenchmarkDotNet.Attributes;
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

		// [Benchmark]
		// public void Previous64()
		// {
		// 	LZ4PrevCodec.Encode(
		// 		_source, 0, _source.Length,
		// 		_target, 0, _target.Length);
		// }
		
		[Benchmark]
		public void Current32()
		{
			LZ4Codec.Enforce32 = true;
			var encoded = LZ4Codec.Encode(
				_source, 0, _source.Length,
				_target, 0, _target.Length);
			LZ4Codec.Decode(
				_target, 0, encoded,
				_source, 0, _source.Length);
			LZ4Codec.Enforce32 = false;
		}
		
		[Benchmark]
		public void CurrentA7()
		{
			LZ4Codec.EnforceA7 = true;
			var encoded = LZ4Codec.Encode(
				_source, 0, _source.Length,
				_target, 0, _target.Length);
			LZ4Codec.Decode(
				_target, 0, encoded,
				_source, 0, _source.Length);
			LZ4Codec.EnforceA7 = false;
		}
		
		[Benchmark]
		public void Current64()
		{
			var encoded = LZ4Codec.Encode(
				_source, 0, _source.Length,
				_target, 0, _target.Length);
			LZ4Codec.Decode(
				_target, 0, encoded,
				_source, 0, _source.Length);
		}
	}
}
