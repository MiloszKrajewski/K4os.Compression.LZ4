using K4os.Compression.LZ4.Engine;
using K4os.Compression.LZ4.Internal;

namespace K4os.Compression.LZ4.Encoders;

// fast encoder context
using LZ4Context = LL.LZ4_stream_t;

/// <summary>
/// LZ4 encoder using dependent blocks with fast compression.
/// </summary>
public unsafe class LZ4FastChainEncoder: LZ4EncoderBase
{
	private PinnedMemory _contextPin;

	private LZ4Context* Context => _contextPin.Reference<LZ4Context>();

	/// <summary>Creates new instance of <see cref="LZ4FastChainEncoder"/></summary>
	/// <param name="blockSize">Block size.</param>
	/// <param name="extraBlocks">Number of extra blocks.</param>
	public LZ4FastChainEncoder(int blockSize, int extraBlocks = 0):
		base(true, blockSize, extraBlocks)
	{
		PinnedMemory.Alloc<LZ4Context>(out _contextPin);
	}

	/// <inheritdoc />
	protected override void ReleaseUnmanaged()
	{
		base.ReleaseUnmanaged();
		_contextPin.Free();
	}

	/// <inheritdoc />
	protected override int EncodeBlock(
		byte* source, int sourceLength, byte* target, int targetLength) =>
		LLxx.LZ4_compress_fast_continue(Context, source, target, sourceLength, targetLength, 1);

	/// <inheritdoc />
	protected override int CopyDict(byte* target, int length) =>
		LL.LZ4_saveDict(Context, target, length);
}