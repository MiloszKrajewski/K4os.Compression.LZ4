#if NET5_0_OR_GREATER

using System;
using System.Diagnostics.CodeAnalysis;
using System.IO.Pipelines;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using K4os.Compression.LZ4.Streams.Abstractions;

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
	public void Write(ref EmptyState state, byte[] buffer, int offset, int length) => 
		ThrowSyncInterfaceNotImplemented();

	/// <inheritdoc />
	public async Task<EmptyState> WriteAsync(
		EmptyState state, byte[] buffer, int offset, int length, CancellationToken token)
	{
		await _writer.WriteAsync(buffer.AsMemory(offset, length), token);
		return state;
	}
	
	/// <inheritdoc />
	public bool CanFlush
	{
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => !_writer.CanGetUnflushedBytes || _writer.UnflushedBytes > 0;
	}

	/// <inheritdoc />
	public void Flush(ref EmptyState state) => 
		ThrowSyncInterfaceNotImplemented();

	/// <inheritdoc />
	public async Task<EmptyState> FlushAsync(EmptyState state, CancellationToken token)
	{
		await _writer.FlushAsync(token);
		return state;
	}
	
	[DoesNotReturn]
	private static void ThrowSyncInterfaceNotImplemented() =>
		throw new NotImplementedException(
			$"{nameof(PipeWriter)} does not implement synchronous interface");
}

#endif