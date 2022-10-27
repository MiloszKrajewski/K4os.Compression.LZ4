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
public readonly struct ByteMemoryAdapter:
	IStreamReader<ReadOnlyMemory<byte>>,
	IStreamReader<Memory<byte>>,
	IStreamWriter<Memory<byte>>
{
	private static int ReadAnyMemory(
		in ReadOnlyMemory<byte> memory,
		byte[] buffer, int offset, int length)
	{
		length = Math.Min(memory.Length, length);
		if (length <= 0) return 0;

		var source = memory.Span.Slice(0, length);
		var target = buffer.AsSpan(offset, length);
		source.CopyTo(target);

		return length;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static int Advance(ref Memory<byte> memory, int length)
	{
		if (length > 0) memory = memory.Slice(length);
		return length;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static int Advance(ref ReadOnlyMemory<byte> memory, int length)
	{
		if (length > 0) memory = memory.Slice(length);
		return length;
	}

	/// <inheritdoc />
	public int Read(
		ref ReadOnlyMemory<byte> state,
		byte[] buffer, int offset, int length) =>
		Advance(ref state, ReadAnyMemory(state, buffer, offset, length));

	/// <inheritdoc />
	public int Read(
		ref Memory<byte> state,
		byte[] buffer, int offset, int length) =>
		Advance(ref state, ReadAnyMemory(state, buffer, offset, length));

	/// <inheritdoc />
	public Task<ReadResult<Memory<byte>>> ReadAsync(
		Memory<byte> state,
		byte[] buffer, int offset, int length,
		CancellationToken token)
	{
		token.ThrowIfCancellationRequested();
		var bytes = Read(ref state, buffer, offset, length);
		return Task.FromResult(ReadResult.Create(state, bytes));
	}

	/// <inheritdoc />
	public Task<ReadResult<ReadOnlyMemory<byte>>> ReadAsync(
		ReadOnlyMemory<byte> state,
		byte[] buffer, int offset, int length,
		CancellationToken token)
	{
		token.ThrowIfCancellationRequested();
		var bytes = Read(ref state, buffer, offset, length);
		return Task.FromResult(ReadResult.Create(state, bytes));
	}

	/// <inheritdoc />
	public void Write(
		ref Memory<byte> state,
		byte[] buffer, int offset, int length)
	{
		if (length <= 0) return;

		if (length > state.Length)
			throw new ArgumentOutOfRangeException(nameof(length));

		var source = buffer.AsSpan(offset, length);
		var target = state.Span.Slice(0, length);
		source.CopyTo(target);

		Advance(ref state, length);
	}

	/// <inheritdoc />
	public Task<Memory<byte>> WriteAsync(
		Memory<byte> state,
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
	public void Flush(ref Memory<byte> state) { }

	/// <inheritdoc />
	public Task<Memory<byte>> FlushAsync(Memory<byte> state, CancellationToken token) => 
		Task.FromResult(state);
}
