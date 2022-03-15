﻿using System;
using System.Buffers;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using K4os.Compression.LZ4.Encoders;
using K4os.Compression.LZ4.Internal;
using K4os.Compression.LZ4.Streams.Internal;

namespace K4os.Compression.LZ4.Streams.NewStreams;

/// <summary>
/// LZ4 Decompression stream handling.
/// </summary>
public partial class FrameDecoder<TStream>: IDisposable where TStream: IStreamReader
{
	private ReaderTools<TStream> _reader;
	private readonly Func<ILZ4Descriptor, ILZ4Decoder> _decoderFactory;

	private ILZ4Descriptor _descriptor;
	private ILZ4Decoder _decoder;

	private byte[] _buffer;
	private int _decoded;

	private long _bytesRead;

	private ref ReaderTools<TStream> Reader => ref _reader;

	/// <summary>Creates new instance <see cref="LZ4DecoderStream"/>.</summary>
	/// <param name="stream">Inner stream.</param>
	/// <param name="decoderFactory">Decoder factory.</param>
	protected FrameDecoder(
		TStream stream,
		Func<ILZ4Descriptor, ILZ4Decoder> decoderFactory)
	{
		_decoderFactory = decoderFactory;
		_reader = new ReaderTools<TStream>(stream);
		_bytesRead = 0;
	}

	private static int MaxBlockSize(int blockSizeCode) =>
		blockSizeCode switch {
			7 => Mem.M4, 6 => Mem.M1, 5 => Mem.K256, 4 => Mem.K64, _ => Mem.K64
		};
	
	private static ArrayPool<byte> ArrayPool => ArrayPool<byte>.Shared;

	private ILZ4Decoder CreateDecoder(ILZ4Descriptor descriptor) =>
		_decoderFactory(descriptor);

	protected void CloseFrame()
	{
		if (_decoder == null)
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
	protected virtual byte[] AllocBuffer(int size) => ArrayPool.Rent(size);

	/// <summary>Releases allocated buffer. <see cref="AllocBuffer"/></summary>
	/// <param name="buffer">Previously allocated buffer.</param>
	protected virtual void ReleaseBuffer(byte[] buffer) => ArrayPool.Return(buffer);

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

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	protected long GetBytesRead() => _bytesRead;

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	protected long GetFrameLength() =>
		GetFrameLength(EmptyToken.Value);

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	protected Task<long> GetFrameLengthAsync(CancellationToken token = default) =>
		GetFrameLength(token);

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	protected int ReadOneByte() =>
		ReadOneByte(EmptyToken.Value);

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	protected Task<int> ReadOneByteAsync(CancellationToken token = default) =>
		ReadOneByte(token);
	
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	protected int ReadManyBytes(Span<byte> buffer, bool interactive = false) =>
		ReadManyBytes(EmptyToken.Value, buffer, interactive);
	
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	protected Task<int> ReadManyBytesAsync(
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

	protected virtual void Dispose(bool disposing)
	{
		if (disposing)
			CloseFrame();
	}

	public void Dispose()
	{
		Dispose(true);
		GC.SuppressFinalize(this);
	}
}
