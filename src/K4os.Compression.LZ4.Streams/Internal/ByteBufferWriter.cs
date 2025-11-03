#if NETSTANDARD2_0 || NET462

using System;
using System.Buffers;

namespace K4os.Compression.LZ4.Streams.Internal;

/// <summary>
/// Buffer writer implementation for older frameworks that don't have ArrayBufferWriter.
/// </summary>
internal sealed class ByteBufferWriter: IBufferWriter<byte>
{
	private byte[] _buffer;
	private int _position;

	private ByteBufferWriter()
	{
		_buffer = new byte[1024];
		_position = 0;
	}

	/// <summary>
	/// Creates a buffer writer suitable for the current framework.
	/// </summary>
	/// <returns>An IBufferWriter&lt;byte&gt; instance.</returns>
	public static ByteBufferWriter Create() => new ByteBufferWriter();

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

#else

using System.Buffers;

namespace K4os.Compression.LZ4.Streams.Internal;

/// <summary>
/// Buffer writer factory for newer frameworks.
/// </summary>
internal static class ByteBufferWriter
{
	/// <summary>
	/// Creates a buffer writer suitable for the current framework.
	/// </summary>
	/// <returns>An IBufferWriter&lt;byte&gt; instance.</returns>
	public static ArrayBufferWriter<byte> Create() => new ArrayBufferWriter<byte>();
}

#endif





