using System;
using System.Buffers;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using K4os.Compression.LZ4.Streams.Abstractions;

namespace K4os.Compression.LZ4.Streams.Adapters;

/// <summary>
/// Stream adapter for any class implementing <see cref="IBufferWriter{T}"/>.
/// It takes actual class, not interface, so it can use struct implementations
/// of <see cref="IBufferWriter{T}"/> for performance reasons.
/// Please note, whole <c>K4os.Compression.LZ4.Streams.Adapters</c> namespace should be considered
/// pubternal - exposed as public but still very likely to change.
/// </summary>
/// <typeparam name="TBufferWriter">Type implementing <see cref="IBufferWriter{T}"/></typeparam>
public readonly struct BufferWriterAdapter<TBufferWriter>:
	IStreamWriter<TBufferWriter>
	where TBufferWriter: IBufferWriter<byte>
{
	/// <inheritdoc />
	public void Write(
		ref TBufferWriter state,
		byte[] buffer, int offset, int length)
	{
		if (length <= 0) return;

		var source = buffer.AsSpan(offset, length);
		var target = state.GetSpan(length);
		source.CopyTo(target);
		state.Advance(length);
	}

	/// <inheritdoc />
	public Task<TBufferWriter> WriteAsync(
		TBufferWriter state,
		byte[] buffer, int offset, int length,
		CancellationToken token)
	{
		Write(ref state, buffer, offset, length);
		return Task.FromResult(state);
	}
	
	/// <inheritdoc />
	public bool CanFlush
	{
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => false;
	}

	/// <inheritdoc />
	public void Flush(ref TBufferWriter state) { }
	
	/// <inheritdoc />
	public Task<TBufferWriter> FlushAsync(TBufferWriter state, CancellationToken token) => 
		Task.FromResult(state);
}
