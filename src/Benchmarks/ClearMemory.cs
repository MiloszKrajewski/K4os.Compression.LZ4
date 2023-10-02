using System;
using System.Buffers;
using System.Runtime.CompilerServices;
using BenchmarkDotNet.Attributes;

namespace Benchmarks
{
	public class ClearMemory
	{
		private byte[] _buffer = null!;

		[Params(127, 1023, 65535)]
		public int Size { get; set; }
		
		[GlobalSetup]
		public void Setup() => _buffer = ArrayPool<byte>.Shared.Rent(Size);

		[Benchmark]
		public unsafe void UnsafeInitBlock()
		{
			fixed (byte* ptr = _buffer)
			{
				Unsafe.InitBlockUnaligned(ptr, 0, (uint)Size);
			}
		}
		
		[Benchmark]
		public unsafe void SpanClear()
		{
			fixed (byte* ptr = _buffer)
			{
				// could straight from array, but this is more realistic
				new Span<byte>(ptr, Size).Clear();
			}
		}

	}
}
