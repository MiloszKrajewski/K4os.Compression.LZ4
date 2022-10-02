using System;
using System.Threading;
using System.Threading.Tasks;

namespace K4os.Compression.LZ4.Streams.Abstractions;

/// <summary>
/// Generic interface for frame/stream decoder for LZ4.
/// </summary>
public interface IFrameDecoder: IDisposable
{
	/// <summary>
	/// Opens frame for reading. Please note, this method is not needed as it will be
	/// called automatically, but it can be used to quickly check if frame is valid. 
	/// </summary>
	/// <returns><c>true</c> if frame was just opened,
	/// <c>false</c> if it was opened before.</returns>
	bool OpenFrame();
	
	/// <summary>Async version of <see cref="OpenFrame"/>.</summary>
	/// <param name="token">Cancellation token.</param>
	/// <returns><c>true</c> if frame was just opened,
	/// <c>false</c> if it was opened before.</returns>
	Task<bool> OpenFrameAsync(CancellationToken token);

	/// <summary>Gets the length of the frame content if it was provided when content was encoded.</summary>
	/// <returns>Frame length, or <c>null</c></returns>
	long? GetFrameLength();
	
	/// <summary>Async version of <see cref="GetFrameLength"/>.</summary>
	/// <param name="token">Cancellation token.</param>
	/// <returns>Frame length, or <c>null</c></returns>
	Task<long?> GetFrameLengthAsync(CancellationToken token);
	
	/// <summary>Reads one byte from LZ4 stream.</summary>
	/// <returns>A byte, or -1 if end of stream.</returns>
	int ReadOneByte();
	
	/// <summary>Reads one byte from LZ4 stream.</summary>
	/// <param name="token">Cancellation token.</param>
	/// <returns>A byte, or -1 if end of stream.</returns>
	Task<int> ReadOneByteAsync(CancellationToken token);
	
	/// <summary>Reads many bytes from LZ4 stream. Return number of bytes actually read.</summary>
	/// <param name="buffer">Byte buffer to read into.</param>
	/// <param name="interactive">if <c>true</c> then returns as soon as some bytes are read,
	/// if <c>false</c> then waits for all bytes being read or end of stream.</param>
	/// <returns>Number of bytes actually read.
	/// <c>0</c> means that end of stream has been reached.</returns>
	int ReadManyBytes(
		Span<byte> buffer, bool interactive = false);

	/// <summary>Reads many bytes from LZ4 stream. Return number of bytes actually read.</summary>
	/// <param name="token">Cancellation token.</param>
	/// <param name="buffer">Byte buffer to read into.</param>
	/// <param name="interactive">if <c>true</c> then returns as soon as some bytes are read,
	/// if <c>false</c> then waits for all bytes being read or end of stream.</param>
	/// <returns>Number of bytes actually read.
	/// <c>0</c> means that end of stream has been reached.</returns>
	Task<int> ReadManyBytesAsync(
		CancellationToken token, Memory<byte> buffer, bool interactive = false);
	
	/// <summary>Returns how many bytes in has been read from stream so far.</summary>
	/// <returns>Number of bytes read in total.</returns>
	long GetBytesRead();
	
	/// <summary>Closes the stream, releases allocated memory.</summary>
	void CloseFrame();
}