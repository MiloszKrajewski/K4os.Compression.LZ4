namespace K4os.Compression.LZ4.Encoders;

/// <summary>
/// Static class with factory methods to create LZ4 decoders.
/// </summary>
public static class LZ4Decoder
{
	/// <summary>Creates appropriate decoder for given parameters.</summary>
	/// <param name="chaining">Dependent blocks.</param>
	/// <param name="blockSize">Block size.</param>
	/// <param name="extraBlocks">Number of extra blocks.</param>
	/// <returns>LZ4 decoder.</returns>
	public static ILZ4Decoder Create(
		bool chaining, int blockSize, int extraBlocks = 0) =>
		!chaining ? CreateBlockDecoder(blockSize) : CreateChainDecoder(blockSize, extraBlocks);

	private static ILZ4Decoder CreateChainDecoder(int blockSize, int extraBlocks) =>
		new LZ4ChainDecoder(blockSize, extraBlocks);

	private static ILZ4Decoder CreateBlockDecoder(int blockSize) =>
		new LZ4BlockDecoder(blockSize);
}