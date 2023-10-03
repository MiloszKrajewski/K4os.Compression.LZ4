using System;
using BenchmarkDotNet.Attributes;
using K4os.Compression.LZ4;
using TestHelpers;
using LZ4PrevCodec = K4os.Compression.LZ4.vPrev.LZ4Codec;

namespace Benchmarks
{
	public class BlockDecompression
	{
		private byte[] _source = null!;
		private byte[] _target = null!;
		
		/*
		|     Method |     Mean |     Error |    StdDev |
		|----------- |---------:|----------:|----------:|
		| Previous64 | 2.038 ms | 0.0048 ms | 0.0040 ms |
		|  Current64 | 2.037 ms | 0.0056 ms | 0.0046 ms |
		|  Current32 | 2.045 ms | 0.0045 ms | 0.0040 ms |
		 */

		[GlobalSetup]
		public void Setup()
		{
			var filename = Tools.FindFile(".corpus/xml");
			var content = File.ReadAllBytes(filename);
			_source = Compress(content);
			_target = new byte[content.Length];
		}
		
		public byte[] Compress(byte[] source)
		{
			var buffer = new byte[LZ4Codec.MaximumOutputSize(source.Length)];
			var encoded = LZ4Codec.Encode(
				source, 0, source.Length,
				buffer, 0, buffer.Length);
			var compressed = new byte[encoded];
			Buffer.BlockCopy(buffer, 0, compressed, 0, encoded);
			return compressed;
		}


		[Benchmark]
		public void Previous64()
		{
			LZ4PrevCodec.Decode(
				_source, 0, _source.Length,
				_target, 0, _target.Length);
		}

		[Benchmark]
		public void Current64()
		{
			LZ4PrevCodec.Decode(
				_source, 0, _source.Length,
				_target, 0, _target.Length);
		}

		[Benchmark]
		public void Current32()
		{
			LZ4Codec.Enforce32 = true;
			LZ4PrevCodec.Decode(
				_source, 0, _source.Length,
				_target, 0, _target.Length);
			LZ4Codec.Enforce32 = false;
		}
	}
}
