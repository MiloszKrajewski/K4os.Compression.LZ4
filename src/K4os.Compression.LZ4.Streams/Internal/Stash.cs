using System.Diagnostics;
using System.Runtime.CompilerServices;
using K4os.Compression.LZ4.Internal;
using K4os.Hash.xxHash;

namespace K4os.Compression.LZ4.Streams.Internal;

internal struct Stash
{
	private byte[] _buffer;
	private readonly int _size;
	private int _head;

	public Stash(): this(32) { }

	public Stash(int size)
	{
		Debug.Assert(size >= 16, "Buffer is too small");

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

	public byte[] Data
	{
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => _buffer;
	}

	public int Head
	{
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => _head;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public int Flush()
	{
		var head = _head;
		_head = 0;
		return head;
	}

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
	public Span<byte> OneByteSpan(byte value)
	{
		var result = OneByteSpan();
		result[0] = value;
		return result;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Memory<byte> OneByteMemory() => _buffer.AsMemory(_size, 1);

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Memory<byte> OneByteMemory(byte value)
	{
		var result = OneByteMemory();
		result.Span[0] = value;
		return result;
	}

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
	public void Poke1(byte value) => PokeN(value);

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void Poke2(ushort value) => PokeN(value);

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void Poke4(uint value) => PokeN(value);

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void Poke8(ulong value) => PokeN(value);

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void TryPoke4(uint? value)
	{
		if (value.HasValue) Poke4(value.Value);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private void ValidateBuffer()
	{
		if (_head >= _size)
			throw new InvalidOperationException($"Buffer too small ({_size})");
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private unsafe void PokeN<T>(T value) where T: struct
	{
		ValidateBuffer();
		Unsafe.CopyBlockUnaligned(
			ref _buffer[_head],
			ref *(byte*)Unsafe.AsPointer(ref value),
			(uint)Unsafe.SizeOf<T>());
		_head += Unsafe.SizeOf<T>();
	}
	
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public uint Digest(int offset = 0) =>
		XXH32.DigestOf(AsSpan(offset));

}
