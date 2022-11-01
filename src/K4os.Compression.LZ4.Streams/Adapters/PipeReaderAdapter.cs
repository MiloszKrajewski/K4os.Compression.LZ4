#if NET5_0_OR_GREATER

using System;
using System.Buffers;
using System.Diagnostics.CodeAnalysis;
using System.IO.Pipelines;
using System.Threading;
using System.Threading.Tasks;
using K4os.Compression.LZ4.Streams.Abstractions;
using ReadResult = K4os.Compression.LZ4.Streams.Abstractions.ReadResult;

namespace K4os.Compression.LZ4.Streams.Adapters;

/// <summary>
/// Stream adapter for <see cref="PipeReader"/>.
/// Please note, whole <c>K4os.Compression.LZ4.Streams.Adapters</c> namespace should be considered
/// pubternal - exposed as public but still very likely to change.
/// </summary>
public readonly struct PipeReaderAdapter: IStreamReader<ReadOnlySequence<byte>>
{
	private readonly PipeReader _reader;

	/// <summary>
	/// Creates new instance of <see cref="PipeReaderAdapter"/>.
	/// </summary>
	/// <param name="reader">Pipe reader.</param>
	public PipeReaderAdapter(PipeReader reader) => _reader = reader;

	/// <inheritdoc />
	public int Read(
		ref ReadOnlySequence<byte> state,
		byte[] buffer, int offset, int length) =>
		throw new NotImplementedException(
			$"{nameof(PipeReader)} does not implement synchronous interface");

	/// <inheritdoc />
	public async Task<ReadResult<ReadOnlySequence<byte>>> ReadAsync(
		ReadOnlySequence<byte> state,
		byte[] buffer, int offset, int length,
		CancellationToken token)
	{
		if (length <= 0)
			return ReadResult.Create(state);

		if (state.IsEmpty)
		{
			state = await HydrateSequence(_reader, length, token);
			if (state.IsEmpty) // still 
				return ReadResult.Create(ReadOnlySequence<byte>.Empty);
		}

		return ReadFromSequence(state, buffer.AsSpan(offset, length));
	}

	private static async Task<ReadOnlySequence<byte>> HydrateSequence(
		PipeReader reader, int length, CancellationToken token)
	{
		var result = await reader.ReadAtLeastAsync(length, token);
		if (result.IsCanceled) ThrowPendingReadsCancelled();
		return result.Buffer;
	}

	private static ReadResult<ReadOnlySequence<byte>> ReadFromSequence(
		ReadOnlySequence<byte> sequence, Span<byte> buffer)
	{
		var sourceSpan = sequence.First.Span;
		var chunk = Math.Min(sourceSpan.Length, buffer.Length);
		sourceSpan.Slice(0, chunk).CopyTo(buffer);
		sequence = sequence.Slice(chunk); // this might be very suboptimal
		return ReadResult.Create(sequence, chunk);
	}
	
	[DoesNotReturn]
	private static void ThrowPendingReadsCancelled() =>
		throw new OperationCanceledException(
			$"Pending {nameof(PipeReader)} operations has been cancelled");
}

#endif
