using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using K4os.Compression.LZ4.Streams.Abstractions;

namespace K4os.Compression.LZ4.Streams.Adapters;

public readonly struct StreamAdapter: IStreamReader, IStreamWriter
{
	private readonly Stream _stream;

	public StreamAdapter(Stream stream) => _stream = stream;

	public int Read(
		byte[] buffer, int offset, int length) =>
		_stream.Read(buffer, offset, length);

	public Task<int> ReadAsync(
		byte[] buffer, int offset, int length, CancellationToken token) =>
		_stream.ReadAsync(buffer, offset, length, token);

	public void Write(
		byte[] buffer, int offset, int length) =>
		_stream.Write(buffer, offset, length);

	public Task WriteAsync(
		byte[] buffer, int offset, int length, CancellationToken token) =>
		_stream.WriteAsync(buffer, offset, length, token);
}