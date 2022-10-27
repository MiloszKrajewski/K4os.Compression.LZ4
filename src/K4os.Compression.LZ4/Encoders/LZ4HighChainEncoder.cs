using K4os.Compression.LZ4.Engine;
using K4os.Compression.LZ4.Internal;

namespace K4os.Compression.LZ4.Encoders;

// high encoder context
using LZ4Context = LL.LZ4_streamHC_t;

/// <summary>
/// LZ4 encoder using dependent blocks with high compression.
/// </summary>
public unsafe class LZ4HighChainEncoder: LZ4EncoderBase
{
	private readonly LZ4Context* _context;

	/// <summary>Creates new instance of <see cref="LZ4HighChainEncoder"/></summary>
	/// <param name="level">Compression level.</param>
	/// <param name="blockSize">Block size.</param>
	/// <param name="extraBlocks">Number of extra blocks.</param>
	public LZ4HighChainEncoder(LZ4Level level, int blockSize, int extraBlocks = 0):
		base(true, blockSize, extraBlocks)
	{
		if (level < LZ4Level.L03_HC) level = LZ4Level.L03_HC;
		if (level > LZ4Level.L12_MAX) level = LZ4Level.L12_MAX;
		_context = LL.LZ4_createStreamHC();
		LL.LZ4_resetStreamHC_fast(_context, (int) level);
	}

	/// <inheritdoc />
	protected override void ReleaseUnmanaged()
	{
		base.ReleaseUnmanaged();
		LL.LZ4_freeStreamHC(_context);
	}

	/// <inheritdoc />
	protected override int EncodeBlock(
		byte* source, int sourceLength, byte* target, int targetLength) =>
		LLxx.LZ4_compress_HC_continue(
			_context, source, target, sourceLength, targetLength);

	/// <inheritdoc />
	protected override int CopyDict(byte* target, int length) =>
		LL.LZ4_saveDictHC(_context, target, length);
}