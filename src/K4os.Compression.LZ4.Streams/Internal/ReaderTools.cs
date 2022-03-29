using System;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using K4os.Compression.LZ4.Streams.Abstractions;
using K4os.Hash.xxHash;

namespace K4os.Compression.LZ4.Streams.Internal;

internal struct ReaderTools<TStream> where TStream: IStreamReader
{
	private readonly TStream _stream;
	private byte[] _buffer;
	private readonly int _size;

	private int _head;
		
	public ReaderTools(TStream stream): this(stream, 32) { }
	
	public ReaderTools(TStream stream, int size)
	{
		Debug.Assert(size >= 16, "Buffer is too small");
			
		_stream = stream;
		_buffer = BufferPool.Alloc(size);
		_size = _buffer.Length - 8;
		_head = 0;
	}
	
	public void Dispose()
	{
		if (_buffer != null) 
			BufferPool.Free(_buffer);
		_buffer = null!;
	}
		
	public TStream Stream => _stream;

	public bool Empty => _head <= 0;

	public int Length => _head;

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public int Clear()
	{
		var result = _head;
		_head = 0;
		return result;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public int Read(
		EmptyToken _, int count, bool optional = false) =>
		_stream.TryReadBlock(_buffer, _head, count, optional);

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Task<int> Read(
		CancellationToken token, int count, bool optional = false) =>
		_stream.TryReadBlockAsync(_buffer, _head, count, optional, token);
		
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public int Read(
		EmptyToken _, 
		byte[] buffer, int offset, int count, 
		bool optional = false) =>
		_stream.TryReadBlock(buffer, offset, count, optional);
		
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Task<int> Read(
		CancellationToken token, 
		byte[] buffer, int offset, int count, 
		bool optional = false) =>
		_stream.TryReadBlockAsync(buffer, offset, count, optional, token);

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public int Advance(int loaded)
	{
		_head += loaded;
		return loaded;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Span<byte> AsSpan(int offset = 0) =>
		_buffer.AsSpan(offset, Math.Max(0, _head - offset));

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public byte OneByteValue() => _buffer[_size];

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Span<byte> OneByteSpan() => _buffer.AsSpan(_size, 1);
		
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Memory<byte> OneByteMemory() => _buffer.AsMemory(_size, 1);

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Span<byte> OneByteBuffer(in EmptyToken _) => OneByteSpan();

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Memory<byte> OneByteBuffer(in CancellationToken _) => OneByteMemory();

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public ulong Last8(int loaded = 0) => 
		BitConverter.ToUInt64(_buffer, (_head += loaded) - 8);

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public uint Last4(int loaded = 0) => 
		BitConverter.ToUInt32(_buffer, (_head += loaded) - 4);
		
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public ushort Last2(int loaded = 0) => 
		BitConverter.ToUInt16(_buffer, (_head += loaded) - 2);
		
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public byte Last1(int loaded = 0) => 
		_buffer[(_head += loaded) - 1];
		
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public uint Digest(int offset = 0) =>
		XXH32.DigestOf(AsSpan(offset));
}