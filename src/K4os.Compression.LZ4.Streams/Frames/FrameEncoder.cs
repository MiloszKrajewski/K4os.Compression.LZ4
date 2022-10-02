using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using K4os.Compression.LZ4.Encoders;
using K4os.Compression.LZ4.Internal;
using K4os.Compression.LZ4.Streams.Abstractions;
using K4os.Compression.LZ4.Streams.Internal;

namespace K4os.Compression.LZ4.Streams.Frames;

/// <summary>
/// LZ4 stream encoder. 
/// </summary>
public partial class FrameEncoder<TStreamWriter, TStreamState>:
	IFrameEncoder
	where TStreamWriter: IStreamWriter<TStreamState>
{
	private readonly TStreamWriter _writer;
	private TStreamState _stream;
	private Stash _stash = new();

	private readonly Func<ILZ4Descriptor, ILZ4Encoder> _encoderFactory;
	private ILZ4Descriptor _descriptor;
	private ILZ4Encoder _encoder;

	private byte[] _buffer;

	private long _bytesWritten;
	
	/// <summary>Creates new instance of <see cref="LZ4EncoderStream"/>.</summary>
	/// <param name="writer">Inner stream.</param>
	/// <param name="stream">Inner stream initial state.</param>
	/// <param name="encoderFactory">LZ4 Encoder factory.</param>
	/// <param name="descriptor">LZ4 settings.</param>
	public FrameEncoder(
		TStreamWriter writer, TStreamState stream,
		Func<ILZ4Descriptor, ILZ4Encoder> encoderFactory,
		ILZ4Descriptor descriptor)
	{
		_writer = writer;
		_stream = stream;
		_descriptor = descriptor;
		_encoderFactory = encoderFactory;
		_bytesWritten = 0;
	}

	/// <summary>
	/// Exposes internal stream state. Existence of this stream is a hack,
	/// and it really shouldn't be here but it is needed for relatively low
	/// level operations (like writing directly to unmanaged memory).
	/// Please, do not use it directly, if don't know what you are doing. 
	/// </summary>
	public TStreamState Stream => _stream;

	[SuppressMessage("ReSharper", "InconsistentNaming")]
	private bool TryStashFrame()
	{
		if (_encoder != null)
			return false;

		_stash.Poke4(0x184D2204);

		var headerOffset = _stash.Head;

		const int versionCode = 0x01;
		var blockChaining = _descriptor.Chaining;
		var blockChecksum = _descriptor.BlockChecksum;
		var contentChecksum = _descriptor.ContentChecksum;
		var hasContentSize = _descriptor.ContentLength.HasValue;
		var hasDictionary = _descriptor.Dictionary.HasValue;

		var FLG =
			(versionCode << 6) |
			((blockChaining ? 0 : 1) << 5) |
			((blockChecksum ? 1 : 0) << 4) |
			((hasContentSize ? 1 : 0) << 3) |
			((contentChecksum ? 1 : 0) << 2) |
			(hasDictionary ? 1 : 0);

		var blockSize = _descriptor.BlockSize;

		var BD = MaxBlockSizeCode(blockSize) << 4;

		_stash.Poke2((ushort)((FLG & 0xFF) | (BD & 0xFF) << 8));

		if (hasContentSize)
			throw NotImplemented(
				"ContentSize feature is not implemented"); // Stash8(contentSize);

		if (hasDictionary)
			throw NotImplemented(
				"Predefined dictionaries feature is not implemented"); // Stash4(dictionaryId);

		var HC = (byte)(_stash.Digest(headerOffset) >> 8);

		_stash.Poke1(HC);

		_encoder = CreateEncoder();
		_buffer = AllocateBuffer(LZ4Codec.MaximumOutputSize(blockSize));

		return true;
	}

	/// <summary>Allocate temporary buffer to store decompressed data.</summary>
	/// <param name="size">Minimum size of the buffer.</param>
	/// <returns>Allocated buffer.</returns>
	protected virtual byte[] AllocateBuffer(int size) => BufferPool.Alloc(size);

	/// <summary>Releases allocated buffer. <see cref="AllocateBuffer"/></summary>
	/// <param name="buffer">Previously allocated buffer.</param>
	protected virtual void ReleaseBuffer(byte[] buffer) => BufferPool.Free(buffer);

	private ILZ4Encoder CreateEncoder()
	{
		var encoder = _encoderFactory(_descriptor);
		if (encoder.BlockSize > _descriptor.BlockSize)
			throw InvalidValue("BlockSize is greater than declared");

		return encoder;
	}

	private BlockInfo TopupAndEncode(
		ReadOnlySpan<byte> buffer, ref int offset, ref int count)
	{
		var action = _encoder.TopupAndEncode(
			buffer.Slice(offset, count),
			_buffer.AsSpan(),
			false, true,
			out var loaded,
			out var encoded);

		_bytesWritten += loaded;
		offset += loaded;
		count -= loaded;

		return new BlockInfo(_buffer, action, encoded);
	}

	private BlockInfo FlushAndEncode()
	{
		var action = _encoder.FlushAndEncode(
			_buffer.AsSpan(), true, out var encoded);

		return new BlockInfo(_buffer, action, encoded);
	}

	private static uint BlockLengthCode(in BlockInfo block) =>
		(uint)block.Length | (block.Compressed ? 0 : 0x80000000);

	// ReSharper disable once UnusedParameter.Local
	// NOTE: block will carry checksum one day
	private uint? BlockChecksum(BlockInfo block)
	{
		if (_descriptor.BlockChecksum)
			throw NotImplemented("BlockChecksum");

		return null;
	}

	private uint? ContentChecksum()
	{
		if (_descriptor.ContentChecksum)
			throw NotImplemented("ContentChecksum");

		return null;
	}

	private int MaxBlockSizeCode(int blockSize) =>
		blockSize <= Mem.K64 ? 4 :
		blockSize <= Mem.K256 ? 5 :
		blockSize <= Mem.M1 ? 6 :
		blockSize <= Mem.M4 ? 7 :
		throw InvalidBlockSize(blockSize);

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public long GetBytesWritten() => _bytesWritten;

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void WriteOneByte(byte value) =>
		WriteOneByte(EmptyToken.Value, value);

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Task WriteOneByteAsync(CancellationToken token, byte value) =>
		WriteOneByte(token, value);

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void WriteManyBytes(ReadOnlySpan<byte> buffer) =>
		WriteManyBytes(EmptyToken.Value, buffer);

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Task WriteManyBytesAsync(CancellationToken token, ReadOnlyMemory<byte> buffer) =>
		WriteManyBytes(token, buffer);

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public bool OpenFrame() => OpenFrame(EmptyToken.Value);

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Task<bool> OpenFrameAsync(CancellationToken token = default) => OpenFrame(token);

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void CloseFrame() => CloseFrame(EmptyToken.Value);

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Task CloseFrameAsync(CancellationToken token = default) =>
		CloseFrame(token);

	protected virtual void Dispose(bool disposing)
	{
		if (!disposing) return;

		CloseFrame();
		_stash.Dispose();
	}

	public void Dispose()
	{
		Dispose(true);
		GC.SuppressFinalize(this);
	}

	#if NETSTANDARD2_1_OR_GREATER || NET5_0_OR_GREATER
	
	public async ValueTask DisposeAsync()
	{
		await CloseFrameAsync();
		GC.SuppressFinalize(this);
	}

	#endif

	// ReSharper disable once UnusedParameter.Local
	private void FlushMeta(EmptyToken _)
	{
		var length = _stash.Flush();
		if (length <= 0) return;

		var buffer = _stash.Data;
		_writer.Write(ref _stream, buffer, 0, length);
	}

	private async Task FlushMeta(CancellationToken token)
	{
		var length = _stash.Flush();
		if (length <= 0) return;

		var buffer = _stash.Data;
		_stream = await _writer.WriteAsync(_stream, buffer, 0, length, token);
	}

	// ReSharper disable once UnusedParameter.Local
	private void WriteData(EmptyToken _, BlockInfo block)
	{
		_writer.Write(ref _stream, block.Buffer, block.Offset, block.Length);
	}

	private async Task WriteData(CancellationToken token, BlockInfo block)
	{
		_stream = await _writer
			.WriteAsync(_stream, block.Buffer, block.Offset, block.Length, token)
			.Weave();
	}
	
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	// ReSharper disable once UnusedParameter.Local
	private Span<byte> OneByteBuffer(in EmptyToken _, byte value) =>
		_stash.OneByteSpan(value);

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	// ReSharper disable once UnusedParameter.Local
	private Memory<byte> OneByteBuffer(in CancellationToken _, byte value) =>
		_stash.OneByteMemory(value);

	private NotImplementedException NotImplemented(string operation) =>
		new($"Feature {operation} has not been implemented in {GetType().Name}");

	private static ArgumentException InvalidValue(string description) =>
		new(description);

	private protected ArgumentException InvalidBlockSize(int blockSize) =>
		InvalidValue($"Invalid block size ${blockSize} for {GetType().Name}");
}
