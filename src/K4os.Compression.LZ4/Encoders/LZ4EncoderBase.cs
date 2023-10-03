using System;
using K4os.Compression.LZ4.Internal;

namespace K4os.Compression.LZ4.Encoders;

/// <summary>
/// Base class for LZ4 encoders. Provides basic functionality shared by
/// <see cref="LZ4BlockEncoder"/>, <see cref="LZ4FastChainEncoder"/>,
/// and <see cref="LZ4HighChainEncoder"/> encoders. Do not used directly.
/// </summary>
public abstract unsafe class LZ4EncoderBase: UnmanagedResources, ILZ4Encoder
{
	private PinnedMemory _inputBufferPin;
	
	private readonly int _inputLength;
	private readonly int _blockSize;

	private int _inputIndex;
	private int _inputPointer;
	
	private byte* InputBuffer => _inputBufferPin.Pointer;

	/// <summary>Creates new instance of encoder.</summary>
	/// <param name="chaining">Needs to be <c>true</c> if using dependent blocks.</param>
	/// <param name="blockSize">Block size.</param>
	/// <param name="extraBlocks">Number of extra blocks.</param>
	protected LZ4EncoderBase(bool chaining, int blockSize, int extraBlocks)
	{
		blockSize = Mem.RoundUp(Math.Max(blockSize, Mem.K1), Mem.K1);
		extraBlocks = Math.Max(extraBlocks, 0);
		var dictSize = chaining ? Mem.K64 : 0;

		_blockSize = blockSize;
		_inputLength = dictSize + (1 + extraBlocks) * blockSize + 32;
		_inputIndex = _inputPointer = 0;
		PinnedMemory.Alloc(out _inputBufferPin, _inputLength + 8, false);
	}

	/// <inheritdoc />
	public int BlockSize => _blockSize;

	/// <inheritdoc />
	public int BytesReady => _inputPointer - _inputIndex;

	/// <inheritdoc />
	public int Topup(byte* source, int length)
	{
		ThrowIfDisposed();

		if (length == 0)
			return 0;

		var spaceLeft = _inputIndex + _blockSize - _inputPointer;
		if (spaceLeft <= 0)
			return 0;

		var chunk = Math.Min(spaceLeft, length);
		Mem.Move(InputBuffer + _inputPointer, source, chunk);
		_inputPointer += chunk;

		return chunk;
	}

	/// <inheritdoc />
	public int Encode(byte* target, int length, bool allowCopy)
	{
		ThrowIfDisposed();

		var sourceLength = _inputPointer - _inputIndex;
		if (sourceLength <= 0)
			return 0;

		var encoded = EncodeBlock(InputBuffer + _inputIndex, sourceLength, target, length);

		if (encoded <= 0)
			throw new InvalidOperationException(
				"Failed to encode chunk. Target buffer too small.");

		if (allowCopy && encoded >= sourceLength)
		{
			Mem.Move(target, InputBuffer + _inputIndex, sourceLength);
			encoded = -sourceLength;
		}

		Commit();

		return encoded;
	}

	private void Commit()
	{
		_inputIndex = _inputPointer;
		if (_inputIndex + _blockSize <= _inputLength)
			return;

		_inputIndex = _inputPointer = CopyDict(InputBuffer, _inputPointer);
	}

	/// <summary>Encodes single block using appropriate algorithm.</summary>
	/// <param name="source">Source buffer.</param>
	/// <param name="sourceLength">Source buffer length.</param>
	/// <param name="target">Target buffer.</param>
	/// <param name="targetLength">Target buffer length.</param>
	/// <returns>Number of bytes actually written to target buffer.</returns>
	protected abstract int EncodeBlock(
		byte* source, int sourceLength, byte* target, int targetLength);

	/// <summary>Copies current dictionary.</summary>
	/// <param name="target">Target buffer.</param>
	/// <param name="dictionaryLength">Dictionary length.</param>
	/// <returns>Dictionary length.</returns>
	protected abstract int CopyDict(byte* target, int dictionaryLength);

	/// <inheritdoc />
	protected override void ReleaseUnmanaged()
	{
		base.ReleaseUnmanaged();
		_inputBufferPin.Free();
	}
}