using System;
using System.Buffers;
using System.Threading;
using System.Threading.Tasks;
using K4os.Compression.LZ4.Streams.Abstractions;

namespace K4os.Compression.LZ4.Streams.Adapters;

/// <summary>
/// Stream adapter for any class implementing <see cref="IBufferWriter{T}"/>.
/// It takes actual class, not interface, so it can use struct implementations
/// of <see cref="IBufferWriter{T}"/> for performance reasons.
/// </summary>
/// <typeparam name="TBufferWriter">Type implementing <see cref="IBufferWriter{T}"/></typeparam>
public readonly struct BufferWriterAdapter<TBufferWriter>:
	IStreamWriter<TBufferWriter>
	where TBufferWriter: IBufferWriter<byte>
{
	/// <inheritdoc />
	public void Write(
		ref TBufferWriter stream,
		byte[] buffer, int offset, int length)
	{
		if (length <= 0) return;

		var source = buffer.AsSpan(offset, length);
		var target = stream.GetSpan(length);
		source.CopyTo(target);
		stream.Advance(length);
	}

	/// <inheritdoc />
	public Task<TBufferWriter> WriteAsync(
		TBufferWriter stream,
		byte[] buffer, int offset, int length,
		CancellationToken token)
	{
		Write(ref stream, buffer, offset, length);
		return Task.FromResult(stream);
	}
}
