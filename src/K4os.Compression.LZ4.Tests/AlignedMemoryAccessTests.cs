using System;
using K4os.Compression.LZ4.Internal;
using Xunit;

namespace K4os.Compression.LZ4.Tests
{
	public class AlignedMemoryAccessTests
	{
		private const int BufferSize = 16 * 1024 * 1024;
		private readonly byte[] _bufferA = new byte[BufferSize + 64];
		private readonly byte[] _bufferB = new byte[BufferSize + 64];

		public AlignedMemoryAccessTests()
		{
			var rng = new Random(0);
			rng.NextBytes(_bufferA);
			rng.NextBytes(_bufferB); // different than A (I hope)
		}

		[Fact]
		public unsafe void Test2()
		{
			fixed (byte* a = &_bufferA[0])
			fixed (byte* b = &_bufferB[0])
				for (var i = 0; i < BufferSize; i++)
				{
					var orig = *(ushort*) (a + i);
					var peekA = Mem.Peek2(a + i);
					Mem.Poke2(b + i, peekA);
					var peekB = Mem.Peek2(b + i);

					Assert.Equal(orig, peekA);
					Assert.Equal(peekA, peekB);
				}
		}

		[Fact]
		public unsafe void Test4()
		{
			fixed (byte* a = &_bufferA[0])
			fixed (byte* b = &_bufferB[0])
				for (var i = 0; i < BufferSize; i++)
				{
					var orig = *(uint*) (a + i);
					var peekA = Mem.Peek4(a + i);
					Mem.Poke4(b + i, peekA);
					var peekB = Mem.Peek4(b + i);

					Assert.Equal(orig, peekA);
					Assert.Equal(peekA, peekB);
				}
		}

		[Fact]
		public unsafe void Test8()
		{
			fixed (byte* a = &_bufferA[0])
			fixed (byte* b = &_bufferB[0])
				for (var i = 0; i < BufferSize; i++)
				{
					var orig = *(ulong*) (a + i);
					var peekA = Mem.Peek8(a + i);
					Mem.Poke8(b + i, peekA);
					var peekB = Mem.Peek8(b + i);

					Assert.Equal(orig, peekA);
					Assert.Equal(peekA, peekB);
				}
		}
	}
}
