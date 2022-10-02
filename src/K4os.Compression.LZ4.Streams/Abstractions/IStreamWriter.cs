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
	/// <summary>Writes byte buffer to underlying stream.</summary>
	/// <param name="stream">Stream state.</param>
	/// <param name="buffer">Byte buffer.</param>
	/// <param name="offset">Offset within buffer.</param>
	/// <param name="length">Number of bytes.</param>
	void Write(
		ref TStreamState stream, 
		byte[] buffer, int offset, int length);

	/// <summary>Writes byte buffer to underlying stream.</summary>
	/// <param name="stream">Stream state.</param>
	/// <param name="buffer">Byte buffer.</param>
	/// <param name="offset">Offset within buffer.</param>
	/// <param name="length">Number of bytes.</param>
	/// <param name="token">Cancellation token.</param>
	/// <returns>New stream state (mutable part).</returns>
	Task<TStreamState> WriteAsync(
		TStreamState stream, 
		byte[] buffer, int offset, int length, 
		CancellationToken token);
}