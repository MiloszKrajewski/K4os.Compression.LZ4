using System;
using K4os.Compression.LZ4.Engine;
using K4os.Compression.LZ4.Internal;

namespace K4os.Compression.LZ4.Encoders;

// fast decoder context
using LZ4Context = LL.LZ4_streamDecode_t;

/// <summary>LZ4 decoder handling dependent blocks.</summary>
public unsafe class LZ4ChainDecoder: UnmanagedResources, ILZ4Decoder
{
	private PinnedMemory _outputBufferPin;
	private PinnedMemory _contextPin;
	
	private readonly int _blockSize;
	private readonly int _outputLength;
	private int _outputIndex;

	private byte* OutputBuffer => _outputBufferPin.Pointer;
	private LZ4Context* Context => _contextPin.Reference<LZ4Context>();

	/// <summary>Creates new instance of <see cref="LZ4ChainDecoder"/>.</summary>
	/// <param name="blockSize">Block size.</param>
	/// <param name="extraBlocks">Number of extra blocks.</param>
	public LZ4ChainDecoder(int blockSize, int extraBlocks)
	{
		blockSize = Mem.RoundUp(Math.Max(blockSize, Mem.K1), Mem.K1);
		extraBlocks = Math.Max(extraBlocks, 0);

		_blockSize = blockSize;
		_outputLength = Mem.K64 + (1 + extraBlocks) * _blockSize + 32;
		_outputIndex = 0;
		PinnedMemory.Alloc<LZ4Context>(out _contextPin);
		PinnedMemory.Alloc(out _outputBufferPin, _outputLength + 8, false);
	}

	/// <inheritdoc />
	public int BlockSize => _blockSize;

	/// <inheritdoc />
	public int BytesReady => _outputIndex;

	/// <inheritdoc />
	public int Decode(byte* source, int length, int blockSize)
	{
		if (blockSize <= 0)
			blockSize = _blockSize;

		Prepare(blockSize);

		var decoded = DecodeBlock(
			source, length, OutputBuffer + _outputIndex, blockSize);

		if (decoded < 0)
			throw new InvalidOperationException();

		_outputIndex += decoded;

		return decoded;
	}

	/// <inheritdoc />
	public int Inject(byte* source, int length)
	{
		if (length <= 0)
			return 0;

		if (length > Math.Max(_blockSize, Mem.K64))
			throw new InvalidOperationException();

		var outputBuffer = OutputBuffer;
		
		if (_outputIndex + length < _outputLength)
		{
			Mem.Move(outputBuffer + _outputIndex, source, length);
			_outputIndex = ApplyDict(_outputIndex + length);
		} 
		else if (length >= Mem.K64)
		{
			Mem.Move(outputBuffer, source, length);
			_outputIndex = ApplyDict(length);
		}
		else
		{
			var tailSize = Math.Min(Mem.K64 - length, _outputIndex);
			Mem.Move(outputBuffer, outputBuffer + _outputIndex - tailSize, tailSize);
			Mem.Move(outputBuffer + tailSize, source, length);
			_outputIndex = ApplyDict(tailSize + length);
		}

		return length;
	}

	/// <inheritdoc />
	public void Drain(byte* target, int offset, int length)
	{
		offset = _outputIndex + offset; // NOTE: negative value
		if (offset < 0 || length < 0 || offset + length > _outputIndex)
			throw new InvalidOperationException();

		Mem.Move(target, OutputBuffer + offset, length);
	}
	
	/// <inheritdoc />
	public byte* Peek(int offset)
	{
		ThrowIfDisposed();

		offset = _outputIndex + offset; // NOTE: negative value
		if (offset < 0 || offset > _outputIndex)
			throw new InvalidOperationException();
		
		return OutputBuffer + offset;
	}

	private void Prepare(int blockSize)
	{
		if (_outputIndex + blockSize <= _outputLength)
			return;

		_outputIndex = CopyDict(_outputIndex);
	}

	private int CopyDict(int index)
	{
		var dictStart = Math.Max(index - Mem.K64, 0);
		var dictSize = index - dictStart;
		Mem.Move(OutputBuffer, OutputBuffer + dictStart, dictSize);
		LL.LZ4_setStreamDecode(Context, OutputBuffer, dictSize);
		return dictSize;
	}

	private int ApplyDict(int index)
	{ 
		var dictStart = Math.Max(index - Mem.K64, 0);
		var dictSize = index - dictStart;
		LL.LZ4_setStreamDecode(Context, OutputBuffer + dictStart, dictSize);
		return index;
	}

	private int DecodeBlock(byte* source, int sourceLength, byte* target, int targetLength) =>
		LLxx.LZ4_decompress_safe_continue(Context, source, target, sourceLength, targetLength);

	/// <inheritdoc />
	protected override void ReleaseUnmanaged()
	{
		base.ReleaseUnmanaged();
		_contextPin.Free();
		_outputBufferPin.Free();
	}
}