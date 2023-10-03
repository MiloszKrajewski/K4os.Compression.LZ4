using System.Runtime.CompilerServices;
using K4os.Compression.LZ4.Encoders;
using K4os.Compression.LZ4.Internal;
using K4os.Compression.LZ4.Streams.Abstractions;
using K4os.Compression.LZ4.Streams.Internal;
using K4os.Hash.xxHash;

namespace K4os.Compression.LZ4.Streams.Frames;

/// <summary>
/// LZ4 Decompression stream handling.
/// </summary>
public partial class LZ4FrameReader<TStreamReader, TStreamState>:
	ILZ4FrameReader
	where TStreamReader: IStreamReader<TStreamState>
{
	private readonly TStreamReader _reader;
	private TStreamState _stream;
	private Stash _stash = new();

	private readonly Func<ILZ4Descriptor, ILZ4Decoder> _decoderFactory;

	private ILZ4Descriptor? _descriptor;
	private ILZ4Decoder? _decoder;
	
	private XXH32.State _contentChecksum;


	private byte[]? _buffer;
	private int _decoded;

	private long _bytesRead;

	/// <summary>Creates new instance <see cref="LZ4DecoderStream"/>.</summary>
	/// <param name="reader">Inner stream.</param>
	/// <param name="stream">Inner stream initial state.</param>
	/// <param name="decoderFactory">Decoder factory.</param>
	public LZ4FrameReader(
		TStreamReader reader,
		TStreamState stream,
		Func<ILZ4Descriptor, ILZ4Decoder> decoderFactory)
	{
		_decoderFactory = decoderFactory;
		_reader = reader;
		_stream = stream;
		_bytesRead = 0;
	}

	/// <summary>
	/// Exposes internal stream state. Existence of this property is a hack,
	/// and it really shouldn't be here but it is needed for relatively low
	/// level operations (like writing directly to unmanaged memory).
	/// Please, do not use it directly, if don't know what you are doing. 
	/// </summary>
	public TStreamState StreamState => _stream;

	private static int MaxBlockSize(int blockSizeCode) =>
		blockSizeCode switch {
			7 => Mem.M4, 6 => Mem.M1, 5 => Mem.K256, 4 => Mem.K64, _ => Mem.K64
		};

	private ILZ4Decoder CreateDecoder(ILZ4Descriptor descriptor) =>
		_decoderFactory(descriptor);

	/// <inheritdoc />
	public void CloseFrame()
	{
		if (_decoder is null)
			return;

		try
		{
			if (_buffer is not null)
				ReleaseBuffer(_buffer);
			_decoder.Dispose();
		}
		finally
		{
			_descriptor = null;
			_buffer = null;
			_decoder = null;
		}
	}

	/// <summary>Allocate temporary buffer to store decompressed data.</summary>
	/// <param name="size">Minimum size of the buffer.</param>
	/// <returns>Allocated buffer.</returns>
	protected virtual byte[] AllocBuffer(int size) => BufferPool.Alloc(size);

	/// <summary>Releases allocated buffer. <see cref="AllocBuffer"/></summary>
	/// <param name="buffer">Previously allocated buffer.</param>
	protected virtual void ReleaseBuffer(byte[] buffer) => BufferPool.Free(buffer);

	private int InjectOrDecode(int blockLength, bool uncompressed) =>
		uncompressed
			? _decoder.Inject(_buffer, 0, blockLength)
			: _decoder.Decode(_buffer, 0, blockLength);

	private bool Drain(Span<byte> buffer, ref int offset, ref int count, ref int read)
	{
		if (_decoded <= 0)
			return true;

		var length = Math.Min(count, _decoded);
		_decoder.Drain(buffer.Slice(offset), -_decoded, length);
		_bytesRead += length;
		_decoded -= length;
		offset += length;
		count -= length;
		read += length;

		return false;
	}

	private void VerifyBlockChecksum(uint expected, int blockLength)
	{
		var actual = XXH32.DigestOf(_buffer, 0, blockLength);
		if (actual != expected) throw InvalidChecksum("block");
	}
	
	private void InitializeContentChecksum() => 
		XXH32.Reset(ref _contentChecksum);
	
	private unsafe void UpdateContentChecksum(int read)
	{
		_decoder.AssertIsNotNull();
		var span = new Span<byte>(_decoder.Peek(-read), read);
		XXH32.Update(ref _contentChecksum, span);
	}

	private void VerifyContentChecksum(uint expected)
	{
		var actual = XXH32.Digest(in _contentChecksum);
		if (expected != actual) throw InvalidChecksum("content");
	}

	/// <inheritdoc />
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public bool OpenFrame() =>
		EnsureHeader(EmptyToken.Value);

	/// <inheritdoc />
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Task<bool> OpenFrameAsync(CancellationToken token) =>
		EnsureHeader(token);

	/// <inheritdoc />
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public long GetBytesRead() => _bytesRead;

	/// <inheritdoc />
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public long? GetFrameLength() =>
		GetFrameLength(EmptyToken.Value);

	/// <inheritdoc />
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Task<long?> GetFrameLengthAsync(CancellationToken token = default) =>
		GetFrameLength(token);

	/// <inheritdoc />
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public int ReadOneByte() =>
		ReadOneByte(EmptyToken.Value);

	/// <inheritdoc />
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Task<int> ReadOneByteAsync(CancellationToken token = default) =>
		ReadOneByte(token);

	/// <inheritdoc />
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public int ReadManyBytes(Span<byte> buffer, bool interactive = false) =>
		ReadManyBytes(EmptyToken.Value, buffer, interactive);

	/// <inheritdoc />
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Task<int> ReadManyBytesAsync(
		CancellationToken token, Memory<byte> buffer, bool interactive = false) =>
		ReadManyBytes(token, buffer, interactive);

	private static NotImplementedException NotImplemented(string feature) =>
		new($"Feature '{feature}' is not implemented");

	private static InvalidDataException InvalidHeaderChecksum() =>
		new("Invalid LZ4 frame header checksum");

	private static InvalidDataException MagicNumberExpected() =>
		new("LZ4 frame magic number expected");

	private static InvalidDataException UnknownFrameVersion(int version) =>
		new($"LZ4 frame version {version} is not supported");
	
	private static InvalidDataException InvalidChecksum(string type) =>
		new($"Invalid {type} checksum");

	/// <summary>
	/// Disposes the decoder. Consecutive attempts to read will fail.
	/// </summary>
	/// <param name="disposing"><c>true</c> is stream is being disposed by user,
	/// <c>true</c> is by garbage collector.</param>
	protected virtual void Dispose(bool disposing)
	{
		if (!disposing) return;

		try
		{
			CloseFrame();
		}
		finally
		{
			_stash.Dispose();
			ReleaseResources();
		}
	}

	/// <summary>
	/// Releases unmanaged resources. 
	/// </summary>
	protected virtual void ReleaseResources() { }

	/// <summary>
	/// Releases unmanaged resources.
	/// </summary>
	/// <returns>Task indicating operation is finished.</returns>
	protected virtual Task ReleaseResourcesAsync() => Task.CompletedTask;

	/// <inheritdoc />
	public void Dispose() { Dispose(true); }

	#if NETSTANDARD2_1_OR_GREATER || NET5_0_OR_GREATER

	/// <inheritdoc />
	public virtual async ValueTask DisposeAsync()
	{
		try
		{
			CloseFrame();
		}
		finally
		{
			_stash.Dispose();
			await ReleaseResourcesAsync().Weave();
		}
	}

	#endif

	// ReSharper disable once UnusedParameter.Local
	private int ReadMeta(EmptyToken _, int length, bool optional = false)
	{
		var buffer = _stash.Data;
		var head = _stash.Head;
		var loaded = _reader.TryReadBlock(ref _stream, buffer, head, length, optional);
		return loaded;
	}

	private async Task<int> ReadMeta(CancellationToken token, int length, bool optional = false)
	{
		var buffer = _stash.Data;
		var head = _stash.Head;
		(_stream, var loaded) = await _reader
			.TryReadBlockAsync(_stream, buffer, head, length, optional, token)
			.Weave();
		return loaded;
	}

	// ReSharper disable once UnusedParameter.Local
	private void ReadData(EmptyToken _, int length)
	{
		_buffer.AssertIsNotNull();
		_reader.TryReadBlock(ref _stream, _buffer, 0, length, false);
	}

	private async Task ReadData(CancellationToken token, int length)
	{
		_buffer.AssertIsNotNull();
		_stream = (await _reader
			.TryReadBlockAsync(_stream, _buffer, 0, length, false, token)
			.Weave()).Stream;
	}
}
