using System;
using System.Threading;
using System.Threading.Tasks;

namespace K4os.Compression.LZ4.Streams.Abstractions;

/// <summary>
/// Generic interface for LZ4 frame/stream writer.
/// </summary>
public interface IFrameEncoder:
	#if NETSTANDARD2_1_OR_GREATER || NET5_0_OR_GREATER
	IAsyncDisposable,
	#endif
	IDisposable
{
	/// <summary>
	/// Opens a stream by reading frame header. Please note, this methods can be called explicitly
	/// but does not need to be called, it will be called automatically if needed. 
	/// </summary>
	/// <returns><c>true</c> if frame has been opened,
	/// or <c>false</c> if it was opened before.</returns>
	bool OpenFrame();

	/// <summary>
	/// Opens a stream by reading frame header. Please note, this methods can be called explicitly
	/// but does not need to be called, it will be called automatically if needed. 
	/// </summary>
	/// <param name="token">Cancellation token.</param>
	/// <returns><c>true</c> if frame has been opened,
	/// or <c>false</c> if it was opened before.</returns>
	Task<bool> OpenFrameAsync(CancellationToken token);

	/// <summary>Writes one byte to stream.</summary>
	/// <param name="value">Byte to be written.</param>
	void WriteOneByte(byte value);

	/// <summary>Writes one byte to stream.</summary>
	/// <param name="token">Cancellation token.</param>
	/// <param name="value">Byte to be written.</param>
	Task WriteOneByteAsync(CancellationToken token, byte value);

	/// <summary>Writes multiple bytes to stream.</summary>
	/// <param name="buffer">Byte buffer.</param>
	void WriteManyBytes(ReadOnlySpan<byte> buffer);

	/// <summary>Writes multiple bytes to stream.</summary>
	/// <param name="token">Cancellation token.</param>
	/// <param name="buffer">Byte buffer.</param>
	Task WriteManyBytesAsync(CancellationToken token, ReadOnlyMemory<byte> buffer);

	/// <summary>Gets number of bytes written.</summary>
	/// <returns>Total number of bytes (before compression).</returns>
	long GetBytesWritten();

	/// <summary>
	/// Closes frame. Frame needs to be closed for stream to by valid, although
	/// this methods does not need to be called explicitly if stream is properly dispose.
	/// </summary>
	void CloseFrame();

	/// <summary>
	/// Closes frame. Frame needs to be closed for stream to by valid, although
	/// this methods does not need to be called explicitly if stream is properly dispose.
	/// </summary>
	/// <param name="token">Cancellation token.</param>
	Task CloseFrameAsync(CancellationToken token);
}
