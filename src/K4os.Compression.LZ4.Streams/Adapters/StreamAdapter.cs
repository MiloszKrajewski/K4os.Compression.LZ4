using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using K4os.Compression.LZ4.Streams.Abstractions;

namespace K4os.Compression.LZ4.Streams.Adapters;

public readonly struct StreamAdapter:
	IStreamReader<Stream>, IStreamWriter<Stream>
{
	public int Read(
		ref Stream stream,
		byte[] buffer, int offset, int length) =>
		stream.Read(buffer, offset, length);

	public Task<ReadResult<Stream>> ReadAsync(
		Stream stream, byte[] buffer, int offset, int length, CancellationToken token) =>
		ReadResult.Some(stream, stream.ReadAsync(buffer, offset, length, token));

	public void Write(ref Stream stream, byte[] buffer, int offset, int length) =>
		stream.Write(buffer, offset, length);

	public async Task<Stream> WriteAsync(
		Stream stream,
		byte[] buffer, int offset, int length,
		CancellationToken token)
	{
		await stream.WriteAsync(buffer, offset, length, token);
		return stream;
	}
}
