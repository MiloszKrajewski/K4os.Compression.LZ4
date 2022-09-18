using System;
using System.Buffers;
using System.Threading;
using System.Threading.Tasks;
using K4os.Compression.LZ4.Streams.Abstractions;

namespace K4os.Compression.LZ4.Streams.Adapters;

public struct BufferWriterAdapter<TBufferWriter>: 
	IStreamWriter<TBufferWriter>
	where TBufferWriter: IBufferWriter<byte>
{
	public void Write(
		ref TBufferWriter state,
		byte[] buffer, int offset, int length)
	{
		if (length <= 0) return;

		var source = buffer.AsSpan(offset, length);
		var target = state.GetSpan(length);
		source.CopyTo(target);
		state.Advance(length);
	}

	public Task<TBufferWriter> WriteAsync(
		TBufferWriter state,
		byte[] buffer, int offset, int length,
		CancellationToken token)
	{
		Write(ref state, buffer, offset, length);
		return Task.FromResult(state);
	}
}
