namespace K4os.Compression.LZ4.Encoders;

/// <summary>
/// Independent block encoder. Produces larger files but uses less memory and
/// gives better performance.
/// </summary>
public unsafe class LZ4BlockEncoder: LZ4EncoderBase
{
	private readonly LZ4Level _level;

	/// <summary>Creates new instance of <see cref="LZ4BlockEncoder"/></summary>
	/// <param name="level">Compression level.</param>
	/// <param name="blockSize">Block size.</param>
	public LZ4BlockEncoder(LZ4Level level, int blockSize): base(false, blockSize, 0) => 
		_level = level;

	/// <inheritdoc />
	protected override int EncodeBlock(
		byte* source, int sourceLength, byte* target, int targetLength) =>
		LZ4Codec.Encode(source, sourceLength, target, targetLength, _level);

	/// <inheritdoc />
	protected override int CopyDict(byte* target, int dictionaryLength) => 0;
}