using System;
using System.Buffers;
using System.IO;
using BenchmarkDotNet.Attributes;
using K4os.Compression.LZ4.Internal;
using K4os.Compression.LZ4.Streams;
using TestHelpers;

namespace K4os.Compression.LZ4.Benchmarks
{
	[MemoryDiagnoser]
	public class InMemoryOldVsNew
	{
		private Memory<byte> _original;
		private byte[] _encoded;
		private byte[] _decoded;

		[Params(128, 1024, 8192)]
		public int Size { get; set; }

		[GlobalSetup]
		public void Setup()
		{
			_original = File.ReadAllBytes(Tools.FindFile(".corpus/dickens")).AsMemory(0, Size);
			_encoded = LZ4Frame.Encode(_original.Span, new BufferWriter()).WrittenMemory.ToArray();
			_decoded = new byte[_original.Length + 32];
		}

		[Benchmark(Baseline = true)]
		public void UseStream()
		{
			PinnedMemory.MaxPooledSize = 0;
			using var source = new MemoryStream(_encoded);
			using var decoder = LZ4Stream.Decode(source);
			_ = decoder.Read(_decoded, 0, _decoded.Length);

		}
		
		[Benchmark]
		public void UseFrameReader()
		{
			PinnedMemory.MaxPooledSize = Mem.M1;
			using var decoder = LZ4Frame.Decode(_encoded);
			_ = decoder.ReadManyBytes(_decoded.AsSpan());
		}
	}
}
