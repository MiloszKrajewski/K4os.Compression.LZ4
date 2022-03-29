using System;
using System.Threading;
using System.Threading.Tasks;

namespace K4os.Compression.LZ4.Streams.Abstractions;

public interface IFrameEncoder:
	#if NETSTANDARD2_1_OR_GREATER || NET5_0_OR_GREATER
	IAsyncDisposable,
	#endif
	IDisposable
{
	bool OpenFrame();
	Task<bool> OpenFrameAsync(CancellationToken token = default);

	void WriteOneByte(byte value);
	Task WriteOneByteAsync(byte value, CancellationToken token = default);
	
	void WriteManyBytes(ReadOnlySpan<byte> buffer);
	Task WriteManyBytesAsync(ReadOnlyMemory<byte> buffer, CancellationToken token = default);
	
	long GetBytesWritten();

	void CloseFrame();
	Task CloseFrameAsync(CancellationToken token = default);
}
