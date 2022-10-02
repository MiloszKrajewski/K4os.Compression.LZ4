using System;
using System.Buffers;
using System.Threading;
using System.Threading.Tasks;
using K4os.Compression.LZ4.Streams.Abstractions;

namespace K4os.Compression.LZ4.Streams.Adapters;

/// <summary>
/// Naive and simplistic implementation of adapter for <see cref="ReadOnlySequence{T}"/>.
/// It might be improved in many ways I believe, but it gives some starting point. 
/// </summary>
public struct ByteSequenceAdapter: IStreamReader<ReadOnlySequence<byte>>
{
	/// <inheritdoc />
	public int Read(
		ref ReadOnlySequence<byte> stream,
		byte[] buffer, int offset, int length)
	{
		if (length <= 0) return 0;
		var span = stream.First.Span;
		var chunk = Math.Min(span.Length, length);
		span.Slice(0, chunk).CopyTo(buffer.AsSpan(offset));
		stream = stream.Slice(chunk); // this might be very suboptimal
		return chunk;
	}

	/// <inheritdoc />
	public Task<ReadResult<ReadOnlySequence<byte>>> ReadAsync(
		ReadOnlySequence<byte> stream,
		byte[] buffer, int offset, int length,
		CancellationToken token)
	{
		token.ThrowIfCancellationRequested();
		var bytes = Read(ref stream, buffer, offset, length);
		return Task.FromResult(ReadResult.Create(stream, bytes));
	}
}
