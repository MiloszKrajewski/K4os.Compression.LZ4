namespace K4os.Compression.LZ4.Encoders
{
	public static class LZ4Decoder
	{
		public static ILZ4Decoder Create(
			bool chaining, int blockSize, int extraBlocks = 0) =>
			!chaining ? CreateBlockDecoder(blockSize) : CreateChainDecoder(blockSize, extraBlocks);

		private static ILZ4Decoder CreateChainDecoder(int blockSize, int extraBlocks) =>
			new LZ4ChainDecoder(blockSize, extraBlocks);

		private static ILZ4Decoder CreateBlockDecoder(int blockSize) =>
			new LZ4BlockDecoder(blockSize);
	}
}
