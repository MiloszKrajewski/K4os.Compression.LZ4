using System;
using System.Threading;
using System.Threading.Tasks;
using K4os.Compression.LZ4.Streams.Abstractions;

namespace K4os.Compression.LZ4.Streams.Adapters;

public class MemoryReaderAdapter: IStreamReader
{
	private readonly ReadOnlyMemory<byte> _memory;
	private int _position;

	public MemoryReaderAdapter(ReadOnlyMemory<byte> memory)
	{
		_memory = memory;
		_position = 0;
	}

	public int Read(byte[] buffer, int offset, int length)
	{
		length = Math.Min(_memory.Length - _position, length);
		if (length <= 0) return 0;

		var source = _memory.Slice(_position, length);
		var target = buffer.AsMemory(offset, length);
		source.CopyTo(target);
		_position += length;
		return length;
	}

	public Task<int> ReadAsync(byte[] buffer, int offset, int length, CancellationToken token) =>
		Task.FromResult(Read(buffer, offset, length));
}
