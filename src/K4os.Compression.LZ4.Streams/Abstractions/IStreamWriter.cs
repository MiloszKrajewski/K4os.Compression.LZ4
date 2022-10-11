using System;
using System.Threading;
using System.Threading.Tasks;

namespace K4os.Compression.LZ4.Streams.Abstractions;

/// <summary>
/// Generic stream writer interface.
/// When implementing custom compression target or decompression source you need to implement
/// this adapter. Please note, that this adapter can be implemented as <c>class</c> or
/// <c>readonly struct</c>. If implemented as <c>struct</c> it cannot have mutable state
/// as it will be lost. Immutable state is allowed but strongly discouraged.
/// Use <typeparamref name="TStreamState"/> instead.
/// </summary>
/// <typeparam name="TStreamState">Mutable part of stream state.</typeparam>
public interface IStreamWriter<TStreamState>
{
	/// <summary>Indicates that writer can and should flush after frame.
	/// Please note, flushing may have negative performance effect but may also lead to
	/// better interactivity between writer and reader, as reader will get new block
	/// available as soon as possible.</summary>
	public bool CanFlush { get; }
	
	/// <summary>Writes byte buffer to underlying stream.</summary>
	/// <param name="state">Stream state.</param>
	/// <param name="buffer">Byte buffer.</param>
	/// <param name="offset">Offset within buffer.</param>
	/// <param name="length">Number of bytes.</param>
	void Write(
		ref TStreamState state, 
		byte[] buffer, int offset, int length);

	/// <summary>Writes byte buffer to underlying stream.</summary>
	/// <param name="state">Stream state.</param>
	/// <param name="buffer">Byte buffer.</param>
	/// <param name="offset">Offset within buffer.</param>
	/// <param name="length">Number of bytes.</param>
	/// <param name="token">Cancellation token.</param>
	/// <returns>New stream state (mutable part).</returns>
	Task<TStreamState> WriteAsync(
		TStreamState state, 
		byte[] buffer, int offset, int length,
		CancellationToken token);

	/// <summary>Flushes buffers to underlying storage. Called only when <see cref="CanFlush"/></summary>
	/// <param name="state">Stream state.</param>
	void Flush(ref TStreamState state);

	/// <summary>Flushes buffers to underlying storage. Called only when <see cref="CanFlush"/></summary>
	/// <param name="state">Stream state.</param>
	/// <param name="token">Cancellation token.</param>
	/// <returns>New stream state (mutable part).</returns>
	Task<TStreamState> FlushAsync(TStreamState state, CancellationToken token);
}