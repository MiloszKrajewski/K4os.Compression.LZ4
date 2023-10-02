using System.Buffers;
using System.Diagnostics.CodeAnalysis;
using System.IO.Pipelines;
using System.Runtime.CompilerServices;
using K4os.Compression.LZ4.Streams.Abstractions;
using K4os.Compression.LZ4.Streams.Internal;
using ReadResult = K4os.Compression.LZ4.Streams.Abstractions.ReadResult;

namespace K4os.Compression.LZ4.Streams.Adapters;

/// <summary>
/// Stream adapter for <see cref="PipeReader"/>.
/// Please note, whole <c>K4os.Compression.LZ4.Streams.Adapters</c> namespace should be considered
/// pubternal - exposed as public but still very likely to change.
/// </summary>
public readonly struct PipeReaderAdapter: IStreamReader<EmptyState>
{
	private readonly PipeReader _reader;

	/// <summary>
	/// Creates new instance of <see cref="PipeReaderAdapter"/>.
	/// </summary>
	/// <param name="reader">Pipe reader.</param>
	public PipeReaderAdapter(PipeReader reader) => _reader = reader;
	
	private static void CheckSyncOverAsync()
	{
		if (SynchronizationContext.Current != null)
			throw new InvalidOperationException(
				"Asynchronous methods cannot be called synchronously when executed in SynchronizationContext.");
	}

	/// <inheritdoc />
	public int Read(
		ref EmptyState state,
		byte[] buffer, int offset, int length)
	{
		CheckSyncOverAsync();

		(state, var result) = ReadAsync(state, buffer, offset, length, CancellationToken.None)
			.GetAwaiter().GetResult();
		return result;
	}

	/// <inheritdoc />
	public async Task<ReadResult<EmptyState>> ReadAsync(
		EmptyState state,
		byte[] buffer, int offset, int length,
		CancellationToken token)
	{
		if (length <= 0)
			return ReadResult.Create(state);

		var sequence = await ReadFromPipe(_reader, length, token).Weave();
		return ReadFromSequence(_reader, sequence, buffer.AsSpan(offset, length));
	}

	private static async Task<ReadOnlySequence<byte>> ReadFromPipe(
		PipeReader reader, int length, CancellationToken token)
	{
		#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_1_OR_GREATER
		var result = await reader.ReadAtLeastAsync(length, token).Weave();
		#else
		_ = length; // ignore
		var result = await reader.ReadAsync(token).Weave();
		#endif
		if (result.IsCanceled) ThrowPendingReadsCancelled();
		return result.Buffer;
	}

	private static ReadResult<EmptyState> ReadFromSequence(
		PipeReader reader, ReadOnlySequence<byte> sequence, Span<byte> buffer)
	{
		var totalRead = 0;
		var bytesLeft = buffer.Length;
		
		while (!sequence.IsEmpty && bytesLeft > 0)
		{
			var sourceSpan = sequence.First.Span;
			var chunk = Math.Min(sourceSpan.Length, bytesLeft);
			sourceSpan.Slice(0, chunk).CopyTo(buffer.Slice(totalRead));
			sequence = sequence.Slice(chunk); // this might be very suboptimal
			totalRead += chunk;
			bytesLeft -= chunk;
		}

		reader.AdvanceTo(sequence.Start);
		return ReadResult.Create(default(EmptyState), totalRead);
	}
	
	[DoesNotReturn]
	[MethodImpl(MethodImplOptions.NoInlining)]
	private static void ThrowPendingReadsCancelled() =>
		throw new OperationCanceledException(
			$"Pending {nameof(PipeReader)} operations has been cancelled");
}

