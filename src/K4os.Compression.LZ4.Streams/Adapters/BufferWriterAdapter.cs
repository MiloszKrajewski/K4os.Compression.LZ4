using System;
using System.Buffers;
using System.Threading;
using System.Threading.Tasks;
using K4os.Compression.LZ4.Streams.Abstractions;

namespace K4os.Compression.LZ4.Streams.Adapters;

public readonly struct BufferWriterAdapter: IStreamWriter
{
	private readonly IBufferWriter<byte> _writer;

	public BufferWriterAdapter(IBufferWriter<byte> writer) => _writer = writer;

	public void Write(byte[] buffer, int offset, int length)
	{
		if (length <= 0) return;

		buffer.AsSpan(offset, length).CopyTo(_writer.GetSpan(length));
		_writer.Advance(length);
	}

	public Task WriteAsync(byte[] buffer, int offset, int length, CancellationToken token)
	{
		Write(buffer, offset, length);
		return LZ4Stream.CompletedTask;
	}
}
