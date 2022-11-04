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
public readonly struct ByteMemoryReadAdapter: IStreamReader<int>
{
	private readonly ReadOnlyMemory<byte> _memory;

	/// <summary>
	/// 
	/// </summary>
	/// <param name="memory"></param>
	public ByteMemoryReadAdapter(ReadOnlyMemory<byte> memory) { _memory = memory; }

	/// <summary>
	/// Copies bytes from span to buffer. Performs all length checks. 
	/// </summary>
	/// <param name="head">Head offset of <see cref="ReadOnlyMemory{T}"/>.</param>
	/// <param name="buffer">Target buffer.</param>
	/// <param name="offset">Offset in target buffer.</param>
	/// <param name="length">Number of bytes to copy.</param>
	/// <returns>Number of bytes actually copied.</returns>
	internal int CopyToBuffer(
		int head, byte[] buffer, int offset, int length)
	{
		length = Math.Min(_memory.Length - head, length);
		if (length <= 0) return 0;

		var source = _memory.Span.Slice(head, length);
		var target = buffer.AsSpan(offset, length);
		source.CopyTo(target);

		return length;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static int Advance(ref int state, int length)
	{
		if (length > 0) state += length;
		return length;
	}

	/// <inheritdoc />
	public int Read(
		ref int state,
		byte[] buffer, int offset, int length) =>
		Advance(ref state, CopyToBuffer(state, buffer, offset, length));

	/// <inheritdoc />
	public Task<ReadResult<int>> ReadAsync(
		int state,
		byte[] buffer, int offset, int length,
		CancellationToken token)
	{
		token.ThrowIfCancellationRequested();
		var bytes = Read(ref state, buffer, offset, length);
		return Task.FromResult(ReadResult.Create(state, bytes));
	}
}
