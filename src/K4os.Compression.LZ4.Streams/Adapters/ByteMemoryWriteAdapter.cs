using System;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using K4os.Compression.LZ4.Streams.Abstractions;

namespace K4os.Compression.LZ4.Streams.Adapters;

/// <summary>
/// Stream adapter for <see cref="ReadOnlyMemory{T}"/> and <see cref="Memory{T}"/>.
/// This class implements <see cref="IStreamWriter{TStreamState}"/> for <see cref="Memory{T}"/>
/// but should be used only in some niche situations, as it is not easy to find out
/// how many bytes has been written, use <see cref="ByteBufferAdapter{TBufferWriter}"/>
/// instead.
/// Please note, whole <c>K4os.Compression.LZ4.Streams.Adapters</c> namespace should be considered
/// pubternal - exposed as public but still very likely to change.
/// </summary>
public readonly struct ByteMemoryWriteAdapter: IStreamWriter<int>
{
	private readonly Memory<byte> _memory;

	/// <summary>
	/// Initializes a new instance of the <see cref="ByteMemoryWriteAdapter"/> class. 
	/// </summary>
	/// <param name="memory">Memory buffer.</param>
	public ByteMemoryWriteAdapter(Memory<byte> memory) { _memory = memory; }

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static void Advance(ref int memory, int length)
	{
		if (length > 0) memory += length;
	}

	/// <inheritdoc />
	public void Write(
		ref int state,
		byte[] buffer, int offset, int length)
	{
		if (length <= 0) return;

		if (length > _memory.Length - state)
			throw new ArgumentOutOfRangeException(nameof(length));

		var source = buffer.AsSpan(offset, length);
		var target = _memory.Span.Slice(state, length);
		source.CopyTo(target);

		Advance(ref state, length);
	}

	/// <inheritdoc />
	public Task<int> WriteAsync(
		int state,
		byte[] buffer, int offset, int length,
		CancellationToken token)
	{
		token.ThrowIfCancellationRequested();
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
	public void Flush(ref int state) { }

	/// <inheritdoc />
	public Task<int> FlushAsync(int state, CancellationToken token) =>
		Task.FromResult(state);
}
