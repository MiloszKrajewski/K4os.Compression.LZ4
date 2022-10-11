using System;
using System.Threading;
using System.Threading.Tasks;

namespace K4os.Compression.LZ4.Streams.Abstractions;

/// <summary>
/// Convenience extension methods for <see cref="IFrameEncoder"/> and <see cref="IFrameDecoder"/>.
/// </summary>
public static class FrameCodecExtensions
{
	/// <summary>Async version of <see cref="IFrameDecoder.OpenFrame"/>.</summary>
	/// <param name="decoder">Decoder.</param>
	/// <returns><c>true</c> if frame was just opened,
	/// <c>false</c> if it was opened before.</returns>
	public static Task<bool> OpenFrameAsync(this IFrameDecoder decoder) =>
		decoder.OpenFrameAsync(CancellationToken.None);

	/// <summary>Async version of <see cref="IFrameDecoder.GetFrameLength"/>.</summary>
	/// <param name="decoder">Decoder.</param>
	/// <returns>Frame length, or <c>null</c></returns>
	public static Task<long?> GetFrameLengthAsync(this IFrameDecoder decoder) =>
		decoder.GetFrameLengthAsync(CancellationToken.None);
	
	/// <summary>Reads one byte from LZ4 stream.</summary>
	/// <param name="decoder">Decoder.</param>
	/// <returns>A byte, or -1 if end of stream.</returns>
	public static Task<int> ReadOneByteAsync(this IFrameDecoder decoder) =>
		decoder.ReadOneByteAsync(CancellationToken.None);

	/// <summary>Reads many bytes from LZ4 stream. Return number of bytes actually read.</summary>
	/// <param name="decoder">Decoder.</param>
	/// <param name="buffer">Byte buffer to read into.</param>
	/// <param name="interactive">if <c>true</c> then returns as soon as some bytes are read,
	/// if <c>false</c> then waits for all bytes being read or end of stream.</param>
	/// <returns>Number of bytes actually read.
	/// <c>0</c> means that end of stream has been reached.</returns>
	public static Task<int> ReadManyBytesAsync(
		this IFrameDecoder decoder, Memory<byte> buffer, bool interactive = false) =>
		decoder.ReadManyBytesAsync(CancellationToken.None, buffer, interactive);
	
	/// <summary>
	/// Opens a stream by reading frame header. Please note, this methods can be called explicitly
	/// but does not need to be called, it will be called automatically if needed. 
	/// </summary>
	/// <param name="encoder">Encoder.</param>
	/// <returns><c>true</c> if frame has been opened, or <c>false</c> if it was opened before.</returns>
	public static Task<bool> OpenFrameAsync(this IFrameEncoder encoder) =>
		encoder.OpenFrameAsync(CancellationToken.None);

	/// <summary>Writes one byte to stream.</summary>
	/// <param name="encoder">Encoder.</param>
	/// <param name="value">Byte to be written.</param>
	public static Task WriteOneByteAsync(
		this IFrameEncoder encoder, byte value) =>
		encoder.WriteOneByteAsync(CancellationToken.None, value);

	/// <summary>Writes multiple bytes to stream.</summary>
	/// <param name="encoder">Encoder.</param>
	/// <param name="buffer">Byte buffer.</param>
	public static Task WriteManyBytesAsync(
		this IFrameEncoder encoder, ReadOnlyMemory<byte> buffer) =>
		encoder.WriteManyBytesAsync(CancellationToken.None, buffer);

	/// <summary>
	/// Closes frame. Frame needs to be closed for stream to by valid, although
	/// this methods does not need to be called explicitly if stream is properly dispose.
	/// </summary>
	/// <param name="encoder">Encoder.</param>
	public static Task CloseFrameAsync(this IFrameEncoder encoder) =>
		encoder.CloseFrameAsync(CancellationToken.None);
}
