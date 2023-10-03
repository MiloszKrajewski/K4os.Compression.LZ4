using System;
using System.Buffers;
using K4os.Compression.LZ4.Internal;

namespace TestHelpers;

public class BufferWriter: IBufferWriter<byte>
{
	private const int BLOCK_SIZE = 1024;

	public static BufferWriter New() => new();
	public static BufferWriter New(int size) => new(size);

	private byte[]? _buffer;
	private int _position;

	public BufferWriter() => _buffer = Mem.Empty;
	public BufferWriter(int size) => _buffer = Reallocate(RoundUp(size));

	public ReadOnlySpan<byte> WrittenSpan => WrittenMemory.Span;
	public ReadOnlyMemory<byte> WrittenMemory => _buffer.AsMemory(0, _position);

	public void Advance(int count) => _position += count;

	public Memory<byte> GetMemory(int sizeHint = 0) =>
		Reallocate(RoundUp(_position + sizeHint)).AsMemory(_position);

	public Span<byte> GetSpan(int sizeHint = 0) => GetMemory(sizeHint).Span;

	private static int RoundUp(int value)
	{
		var size = BLOCK_SIZE;
		while (size < value) size += size >> 1;
		return size;
	}

	private byte[] Reallocate(int size)
	{
		if (_buffer is null || _buffer.Length < size)
			Array.Resize(ref _buffer, size);
		return _buffer;
	}
}