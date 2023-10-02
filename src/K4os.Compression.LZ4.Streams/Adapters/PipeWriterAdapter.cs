using System.IO.Pipelines;
using K4os.Compression.LZ4.Streams.Abstractions;
using K4os.Compression.LZ4.Streams.Internal;

namespace K4os.Compression.LZ4.Streams.Adapters;

/// <summary>
/// LZ4 stream adapter for <see cref="PipeReader"/>.
/// Please note, whole <c>K4os.Compression.LZ4.Streams.Adapters</c> namespace should be considered
/// pubternal - exposed as public but still very likely to change.
/// </summary>
public readonly struct PipeWriterAdapter: IStreamWriter<EmptyState>
{
	private readonly PipeWriter _writer;

	/// <summary>
	/// Creates new instance of <see cref="PipeWriterAdapter"/>.
	/// </summary>
	/// <param name="writer">Pipe writer.</param>
	public PipeWriterAdapter(PipeWriter writer) { _writer = writer; }

	/// <inheritdoc />
	public void Write(ref EmptyState state, byte[] buffer, int offset, int length)
	{
		CheckSyncOverAsync();
		state = WriteAsync(state, buffer, offset, length, CancellationToken.None)
			.GetAwaiter().GetResult();
	}

	/// <inheritdoc />
	public async Task<EmptyState> WriteAsync(
		EmptyState state, byte[] buffer, int offset, int length, CancellationToken token)
	{
		await _writer.WriteAsync(buffer.AsMemory(offset, length), token).Weave();
		return state;
	}

	/// <inheritdoc />
	#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_1_OR_GREATER
	public bool CanFlush => !_writer.CanGetUnflushedBytes || _writer.UnflushedBytes > 0;
	#else
	public bool CanFlush => true;
	#endif

	/// <inheritdoc />
	public void Flush(ref EmptyState state)
	{
		CheckSyncOverAsync();
		state = FlushAsync(state, CancellationToken.None)
			.GetAwaiter().GetResult();
	}

	/// <inheritdoc />
	public async Task<EmptyState> FlushAsync(EmptyState state, CancellationToken token)
	{
		await _writer.FlushAsync(token).Weave();
		return state;
	}

	private static void CheckSyncOverAsync()
	{
		if (SynchronizationContext.Current != null)
			throw new InvalidOperationException(
				"Asynchronous methods cannot be called synchronously when executed in SynchronizationContext.");
	}
}
