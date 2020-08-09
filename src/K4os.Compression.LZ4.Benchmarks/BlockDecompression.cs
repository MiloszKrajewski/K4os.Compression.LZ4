using System;
using System.IO;
using BenchmarkDotNet.Attributes;
using TestHelpers;
using LZ4PrevCodec = K4os.Compression.LZ4.vPrev.LZ4Codec;

namespace K4os.Compression.LZ4.Benchmarks
{
	public class BlockDecompression
	{
		private byte[] _source;
		private byte[] _target;

		[GlobalSetup]
		public void Setup()
		{
			var filename = Tools.FindFile(".corpus/xml");
			var content = File.ReadAllBytes(filename);
			var buffer = new byte[LZ4Codec.MaximumOutputSize(content.Length)];
			var sourceLength = LZ4Codec.Encode(
				content, 0, content.Length, 
				buffer, 0, buffer.Length);
			_source = new byte[sourceLength];
			Array.Copy(buffer, _source, sourceLength);
			_target = new byte[content.Length];
		}

		[Benchmark]
		public void Previous64()
		{
			LZ4PrevCodec.Decode(
				_source, 0, _source.Length,
				_target, 0, _target.Length);
		}
		
		// [Benchmark]
		// public void Current32()
		// {
		// 	LZ4Codec.Enforce32 = true;
		// 	LZ4Codec.Encode(
		// 		_source, 0, _source.Length,
		// 		_target, 0, _target.Length);
		// 	LZ4Codec.Enforce32 = false;
		// }
		
		[Benchmark]
		public void Current64()
		{
			LZ4Codec.Decode(
				_source, 0, _source.Length,
				_target, 0, _target.Length);
		}
	}
}
