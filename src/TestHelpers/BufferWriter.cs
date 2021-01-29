using System;
using System.Buffers;

namespace TestHelpers
{
	public class BufferWriter: IBufferWriter<byte>
	{
		private const int BLOCK_SIZE = 1024;

		public static BufferWriter New() => new BufferWriter();

		private byte[] _buffer = new byte[0];
		private int _position;

		public ReadOnlySpan<byte> WrittenSpan => _buffer.AsSpan(0, _position);

		public void Advance(int count) => _position += count;

		public Memory<byte> GetMemory(int sizeHint = 0) =>
			Reallocate(RoundUp(_position + sizeHint)).AsMemory(_position);
		
		public Span<byte> GetSpan(int sizeHint = 0) => GetMemory(sizeHint).Span;
		
		private static int RoundUp(int value) => 
			(value + BLOCK_SIZE - 1) / BLOCK_SIZE * BLOCK_SIZE;

		private byte[] Reallocate(int size)
		{
			if (_buffer.Length < size) Array.Resize(ref _buffer, size);
			return _buffer;
		}
	}
}
