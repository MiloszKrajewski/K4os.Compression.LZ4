namespace K4os.Compression.LZ4.Encoders
{
	public static class LZ4Encoder
	{
		public static ILZ4Encoder Create(
			bool chaining, LZ4Level level, int blockSize, int extraBlocks = 0) =>
			!chaining ? CreateBlockEncoder(level, blockSize) :
			level == LZ4Level.L00_FAST ? CreateFastEncoder(blockSize, extraBlocks) :
			CreateHighEncoder(level, blockSize, extraBlocks);

		private static ILZ4Encoder CreateBlockEncoder(LZ4Level level, int blockSize) =>
			new LZ4BlockEncoder(level, blockSize);

		private static ILZ4Encoder CreateFastEncoder(int blockSize, int extraBlocks) =>
			new LZ4FastChainEncoder(blockSize, extraBlocks);

		private static ILZ4Encoder CreateHighEncoder(
			LZ4Level level, int blockSize, int extraBlocks) =>
			new LZ4HighChainEncoder(level, blockSize, extraBlocks);
	}
}
