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
	private LZ4FastDictionary _dictionary;

	private LZ4Context* Context => _contextPin.Reference<LZ4Context>();

	/// <summary>Creates new instance of <see cref="LZ4FastChainEncoder"/></summary>
	/// <param name="blockSize">Block size.</param>
	/// <param name="extraBlocks">Number of extra blocks.</param>
	public LZ4FastChainEncoder(int blockSize, int extraBlocks = 0):
		base(true, blockSize, extraBlocks)
	{
		PinnedMemory.Alloc<LZ4Context>(out _contextPin);
	}

	/// <summary>Creates new instance of encoder starting with preset dictionary.</summary>
	/// <param name="blockSize">Block size.</param>
	/// <param name="extraBlocks">Number of extra blocks. With a preset dictionary the 64KB history
	/// window is full from the first block, so with 0 extra blocks every block moves 64KB of history
	/// to the start of the buffer; a few extra blocks (e.g. 4) amortize that copy.</param>
	/// <param name="dictionary">Preset dictionary (loaded once, never modified by the encoder).</param>
	public LZ4FastChainEncoder(int blockSize, int extraBlocks, LZ4FastDictionary dictionary):
		this(blockSize, extraBlocks)
	{
		_dictionary = dictionary;
		Reset();
	}

	/// <summary>Preset dictionary (<c>null</c> if there is none).</summary>
	public LZ4FastDictionary Dictionary => _dictionary;

	/// <summary>
	/// Resets encoder to its initial state: discards buffered input and history,
	/// so next block is encoded as the first one (against preset dictionary, if any).
	/// </summary>
	public void Reset() => Reset(_dictionary);

	/// <summary>
	/// Resets encoder to its initial state, starting from given preset dictionary from now on
	/// (also for subsequent <see cref="Reset()"/> calls).
	/// </summary>
	/// <param name="dictionary">Preset dictionary (<c>null</c> for none).</param>
	public void Reset(LZ4FastDictionary dictionary)
	{
		ThrowIfDisposed();
		_dictionary = dictionary;
		if (_dictionary is null)
		{
			LL.LZ4_initStream(Context);
			ResetInput(0);
		}
		else
		{
			_dictionary.ApplyTo(Context, InputBuffer);
			ResetInput(_dictionary.Length);
		}
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