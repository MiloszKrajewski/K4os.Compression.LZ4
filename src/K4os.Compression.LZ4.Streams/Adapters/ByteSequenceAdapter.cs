using System.Buffers;
using K4os.Compression.LZ4.Streams.Abstractions;

namespace K4os.Compression.LZ4.Streams.Adapters;

/// <summary>
/// Naive and simplistic implementation of adapter for <see cref="ReadOnlySequence{T}"/>.
/// It might be improved in many ways I believe, but it gives some starting point.
/// Please note, whole <c>K4os.Compression.LZ4.Streams.Adapters</c> namespace should be considered
/// pubternal - exposed as public but still very likely to change.
/// </summary>
public struct ByteSequenceAdapter: IStreamReader<ReadOnlySequence<byte>>
{
	/// <inheritdoc />
	public int Read(
		ref ReadOnlySequence<byte> state,
		byte[] buffer, int offset, int length)
	{
		if (length <= 0) return 0;
		var span = state.First.Span;
		var chunk = Math.Min(span.Length, length);
		span.Slice(0, chunk).CopyTo(buffer.AsSpan(offset));
		state = state.Slice(chunk); // this might be very suboptimal
		return chunk;
	}

	/// <inheritdoc />
	public Task<ReadResult<ReadOnlySequence<byte>>> ReadAsync(
		ReadOnlySequence<byte> state,
		byte[] buffer, int offset, int length,
		CancellationToken token)
	{
		token.ThrowIfCancellationRequested();
		var bytes = Read(ref state, buffer, offset, length);
		return Task.FromResult(ReadResult.Create(state, bytes));
	}
}
