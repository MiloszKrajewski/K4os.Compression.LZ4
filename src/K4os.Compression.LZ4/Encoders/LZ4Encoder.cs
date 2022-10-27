namespace K4os.Compression.LZ4.Encoders;

/// <summary>
/// Static class with factory method to create LZ4 encoders.
/// </summary>
public static class LZ4Encoder
{
	/// <summary>Creates appropriate decoder for given parameters.</summary>
	/// <param name="chaining">Dependent blocks.</param>
	/// <param name="level">Compression level.</param>
	/// <param name="blockSize">Block size.</param>
	/// <param name="extraBlocks">Number of extra blocks.</param>
	/// <returns>LZ4 encoder.</returns>
	public static ILZ4Encoder Create(
		bool chaining, LZ4Level level, int blockSize, int extraBlocks = 0) =>
		!chaining ? CreateBlockEncoder(level, blockSize) :
			level < LZ4Level.L03_HC 
				? CreateFastEncoder(blockSize, extraBlocks) 
				: CreateHighEncoder(level, blockSize, extraBlocks);

	private static ILZ4Encoder CreateBlockEncoder(LZ4Level level, int blockSize) =>
		new LZ4BlockEncoder(level, blockSize);

	private static ILZ4Encoder CreateFastEncoder(int blockSize, int extraBlocks) =>
		new LZ4FastChainEncoder(blockSize, extraBlocks);

	private static ILZ4Encoder CreateHighEncoder(
		LZ4Level level, int blockSize, int extraBlocks) => 
		new LZ4HighChainEncoder(level, blockSize, extraBlocks);
}