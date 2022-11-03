﻿using System;
using K4os.Compression.LZ4.Internal;

namespace K4os.Compression.LZ4.Encoders;

/// <summary>
/// LZ4 decoder used with independent blocks mode. Plase note, that it will fail
/// if input data has been compressed with chained blocks
/// (<see cref="LZ4FastChainEncoder"/> and <see cref="LZ4HighChainEncoder"/>)
/// </summary>
public unsafe class LZ4BlockDecoder: UnmanagedResources, ILZ4Decoder
{
	private readonly int _blockSize;
	private readonly int _outputLength;
	private int _outputIndex;
	private readonly byte* _outputBuffer;

	/// <inheritdoc />
	public int BlockSize => _blockSize;

	/// <inheritdoc />
	public int BytesReady => _outputIndex;

	/// <summary>Creates new instance of block decoder.</summary>
	/// <param name="blockSize">Block size. Must be equal or greater to one used for compression.</param>
	public LZ4BlockDecoder(int blockSize)
	{
		blockSize = Mem.RoundUp(Math.Max(blockSize, Mem.K1), Mem.K1);
		_blockSize = blockSize;
		_outputLength = _blockSize + 8;
		_outputIndex = 0;
		_outputBuffer = (byte*) Mem.Alloc(_outputLength + 8);
	}

	/// <inheritdoc />
	public int Decode(byte* source, int length, int blockSize = 0)
	{
		ThrowIfDisposed();
			
		if (blockSize <= 0)
			blockSize = _blockSize;

		if (blockSize > _blockSize)
			throw new InvalidOperationException();

		var decoded = LZ4Codec.Decode(source, length, _outputBuffer, _outputLength);
		if (decoded < 0)
			throw new InvalidOperationException();

		_outputIndex = decoded;
		return _outputIndex;
	}

	/// <inheritdoc />
	public int Inject(byte* source, int length)
	{
		ThrowIfDisposed();
			
		if (length <= 0)
			return _outputIndex = 0;

		if (length > _outputLength)
			throw new InvalidOperationException();

		Mem.Move(_outputBuffer, source, length);
		_outputIndex = length;
		return length;
	}

	/// <inheritdoc />
	public void Drain(byte* target, int offset, int length)
	{
		ThrowIfDisposed();

		offset = _outputIndex + offset; // NOTE: negative value
		if (offset < 0 || length < 0 || offset + length > _outputIndex)
			throw new InvalidOperationException();

		Mem.Move(target, _outputBuffer + offset, length);
	}

	/// <inheritdoc />
	protected override void ReleaseUnmanaged()
	{
		base.ReleaseUnmanaged();
		Mem.Free(_outputBuffer);
	}
}