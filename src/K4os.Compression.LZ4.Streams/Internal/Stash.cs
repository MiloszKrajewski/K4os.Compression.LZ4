using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace K4os.Compression.LZ4.Streams.Internal
{
	internal struct Stash
	{
		private readonly Stream _stream;
		private readonly byte[] _buffer;
		private int _head;
		private readonly int _size;

		public Stash(Stream innerStream, int bufferSize)
		{
			_stream = innerStream;
			_size = bufferSize;
			_buffer = new byte[bufferSize + 8];
			_head = 0;
		}

		public bool Empty => _head <= 0;

		public int Length => _head;

		public int Clear()
		{
			var result = _head;
			_head = 0;
			return result;
		}

		public void Flush(EmptyToken _)
		{
			var length = Clear();
			if (length <= 0) return;

			_stream.Write(_buffer, 0, length);
		}

		public Task Flush(CancellationToken token)
		{
			var length = Clear();
			return length <= 0
				? LZ4Stream.CompletedTask
				: _stream.WriteAsync(_buffer, 0, length, token);
		}

		public int Load(
			EmptyToken _, int count, bool optional = false)
		{
			var read = _stream.TryReadBlock(_buffer, _head, count, optional);
			_head += read;
			return read;
		}

		public async Task<int> Load(
			CancellationToken token, int count, bool optional = false)
		{
			var read = await _stream
				.TryReadBlockAsync(_buffer, _head, count, optional, token)
				.Weave();
			_head += read;
			return read;
		}

		public void Stash1(byte value)
		{
			_buffer[_head + 0] = value;
			_head++;
		}

		public void Stash2(ushort value)
		{
			_buffer[_head + 0] = (byte) (value >> 0);
			_buffer[_head + 1] = (byte) (value >> 8);
			_head += 2;
		}

		public void Stash4(uint value)
		{
			_buffer[_head + 0] = (byte) (value >> 0);
			_buffer[_head + 1] = (byte) (value >> 8);
			_buffer[_head + 2] = (byte) (value >> 16);
			_buffer[_head + 3] = (byte) (value >> 24);
			_head += 4;
		}

		public void TryStash4(uint? value)
		{
			if (!value.HasValue) return;

			Stash4(value.Value);
		}

		public void Stash8(ulong value)
		{
			_buffer[_head + 0] = (byte) (value >> 0);
			_buffer[_head + 1] = (byte) (value >> 8);
			_buffer[_head + 2] = (byte) (value >> 16);
			_buffer[_head + 3] = (byte) (value >> 24);
			_buffer[_head + 4] = (byte) (value >> 32);
			_buffer[_head + 5] = (byte) (value >> 40);
			_buffer[_head + 6] = (byte) (value >> 48);
			_buffer[_head + 7] = (byte) (value >> 56);
			_head += 8;
		}

		public Span<byte> AsSpan(int offset = 0) =>
			_buffer.AsSpan(offset, Math.Max(0, _head - offset));

		public byte OneByteValue() => _buffer[_size];

		public Span<byte> OneByteSpan() => _buffer.AsSpan(_size, 1);

		public Span<byte> OneByteSpan(byte value)
		{
			_buffer[_size] = value;
			return OneByteSpan();
		}

		public ulong Last8() => BitConverter.ToUInt64(_buffer, _head - 8);
		public uint Last4() => BitConverter.ToUInt32(_buffer, _head - 4);
		public ushort Last2() => BitConverter.ToUInt16(_buffer, _head - 2);
		public byte Last1() => _buffer[_head - 1];
	}
}
