using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using K4os.Compression.LZ4.Streams.Internal;
using K4os.Hash.xxHash;

namespace K4os.Compression.LZ4.Streams.NewStreams
{
	internal struct WriterTools<TStream> where TStream: IStreamWriter
	{
		private readonly TStream _stream;
		private readonly byte[] _buffer;
		private readonly int _size;
		
		private int _head;

		public WriterTools(TStream stream): this(stream, 32) { }

		public WriterTools(TStream stream, int size)
		{
			Debug.Assert(size >= 16, "Buffer is too small");

			_stream = stream;
			_buffer = new byte[size];
			_size = size - 8;
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
		
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void Write(
			EmptyToken _, byte[] blockBuffer, int blockOffset, int blockLength) =>
			_stream.Write(blockBuffer, blockOffset, blockLength);

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public Task Write(
			CancellationToken token, byte[] blockBuffer, int blockOffset, int blockLength) =>
			_stream.WriteAsync(blockBuffer, blockOffset, blockLength, token);
		
		public void Flush(EmptyToken token)
		{
			var length = Clear();
			if (length <= 0) return;

			Write(token, _buffer, 0, length);
		}

		public Task Flush(CancellationToken token)
		{
			var length = Clear();
			return length <= 0
				? LZ4Stream.CompletedTask
				: Write(token, _buffer, 0, length);
		}
		
		public int Advance(int loaded)
		{
			_head += loaded;
			return loaded;
		}
		
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public Span<byte> AsSpan(int offset = 0) =>
			_buffer.AsSpan(offset, Math.Max(0, _head - offset));

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public Span<byte> OneByteSpan() => _buffer.AsSpan(_size, 1);
		
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public Memory<byte> OneByteMemory() => _buffer.AsMemory(_size, 1);

		public Span<byte> OneByteSpan(byte value)
		{
			var result = OneByteSpan();
			result[0] = value;
			return result;
		}
		
		public Memory<byte> OneByteMemory(byte value)
		{
			var result = OneByteMemory();
			result.Span[0] = value;
			return result;
		}
		
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		// ReSharper disable once UnusedParameter.Local
		public Span<byte> OneByteBuffer(in EmptyToken _, byte value) => 
			OneByteSpan(value);
		
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		// ReSharper disable once UnusedParameter.Local
		public Memory<byte> OneByteBuffer(in CancellationToken _, byte value) => 
			OneByteMemory(value);

		public uint Digest(int offset = 0) =>
			XXH32.DigestOf(AsSpan(offset));
		
		public void Poke1(byte value)
		{
			#warning can be better
			_buffer[_head + 0] = value;
			_head++;
		}

		public void Poke2(ushort value)
		{
			#warning can be better
			_buffer[_head + 0] = (byte) (value >> 0);
			_buffer[_head + 1] = (byte) (value >> 8);
			_head += 2;
		}

		public void Poke4(uint value)
		{
			#warning can be better
			_buffer[_head + 0] = (byte) (value >> 0);
			_buffer[_head + 1] = (byte) (value >> 8);
			_buffer[_head + 2] = (byte) (value >> 16);
			_buffer[_head + 3] = (byte) (value >> 24);
			_head += 4;
		}

		public void TryPoke4(uint? value)
		{
			if (!value.HasValue) return;

			Poke4(value.Value);
		}

		public void Poke8(ulong value)
		{
			#warning can be better
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
	}
}
