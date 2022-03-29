using System;
using System.Threading;
using System.Threading.Tasks;

namespace K4os.Compression.LZ4.Streams.Abstractions;

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
	Task<long?> GetFrameLengthAsync(CancellationToken token = default);
	
	int ReadOneByte();
	Task<int> ReadOneByteAsync(CancellationToken token = default);
	
	int ReadManyBytes(
		Span<byte> buffer, bool interactive = false);
	Task<int> ReadManyBytesAsync(
		CancellationToken token, Memory<byte> buffer, bool interactive = false);
	Task<int> ReadManyBytesAsync(
		Memory<byte> buffer, bool interactive = false);
	
	long GetBytesRead();
	
	void CloseFrame();
}
