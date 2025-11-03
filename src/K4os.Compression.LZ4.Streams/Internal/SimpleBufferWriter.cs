#if NETSTANDARD2_0 || NET462

using System;
using System.Buffers;

namespace K4os.Compression.LZ4.Streams.Internal;

/// <summary>
/// Simple buffer writer implementation for older frameworks that don't have ArrayBufferWriter.
/// </summary>
internal class SimpleBufferWriter: IBufferWriter<byte>
{
	private byte[] _buffer;
	private int _position;

	public SimpleBufferWriter()
	{
		_buffer = new byte[4096];
		_position = 0;
	}

	public ReadOnlyMemory<byte> WrittenMemory => _buffer.AsMemory(0, _position);

	public void Advance(int count)
	{
		if (count < 0)
			throw new ArgumentOutOfRangeException(nameof(count));
		if (_position > _buffer.Length - count)
			throw new InvalidOperationException("Cannot advance past the end of the buffer.");
		_position += count;
	}

	public Memory<byte> GetMemory(int sizeHint = 0)
	{
		if (sizeHint < 0)
			throw new ArgumentOutOfRangeException(nameof(sizeHint));
		EnsureCapacity(sizeHint);
		return _buffer.AsMemory(_position);
	}

	public Span<byte> GetSpan(int sizeHint = 0) => GetMemory(sizeHint).Span;

	private void EnsureCapacity(int sizeHint)
	{
		var requiredSize = _position + sizeHint;
		var bufferLength = _buffer.Length;
		if (bufferLength >= requiredSize)
			return;

		var newSize = Math.Max(bufferLength + (bufferLength >> 1), requiredSize);
		Array.Resize(ref _buffer, newSize);
	}
}

#endif
