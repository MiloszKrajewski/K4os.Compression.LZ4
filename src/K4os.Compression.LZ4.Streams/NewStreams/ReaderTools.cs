using System;
using System.Threading;
using System.Threading.Tasks;
using K4os.Compression.LZ4.Streams.Internal;
using K4os.Hash.xxHash;

namespace K4os.Compression.LZ4.Streams.NewStreams
{
	internal struct ReaderTools<TStream> 
		where TStream: IStreamReader
	{
		private readonly TStream _stream;
		private readonly byte[] _buffer;
		private int _head;
		private readonly int _size;

		public ReaderTools(TStream stream, int bufferSize)
		{
			_stream = stream;
			_size = bufferSize;
			_buffer = new byte[bufferSize + 8];
			_head = 0;
		}
		
		public TStream Stream => _stream;

		public bool Empty => _head <= 0;

		public int Length => _head;

		public int Clear()
		{
			var result = _head;
			_head = 0;
			return result;
		}

		public int Load(
			EmptyToken _, int count, bool optional = false) =>
			_stream.TryReadBlock(_buffer, _head, count, optional);

		public Task<int> Load(
			CancellationToken token, int count, bool optional = false) =>
			_stream.TryReadBlockAsync(_buffer, _head, count, optional, token);

		public int Advance(int loaded)
		{
			_head += loaded;
			return loaded;
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

		public ulong Last8(int loaded = 0) => 
			BitConverter.ToUInt64(_buffer, (_head += loaded) - 8);

		public uint Last4(int loaded = 0) => 
			BitConverter.ToUInt32(_buffer, (_head += loaded) - 4);
		
		public ushort Last2(int loaded = 0) => 
			BitConverter.ToUInt16(_buffer, (_head += loaded) - 2);
		
		public byte Last1(int loaded = 0) => 
			_buffer[(_head += loaded) - 1];
		
		public uint Digest(int offset = 0) =>
			XXH32.DigestOf(AsSpan(offset));
	}
}
