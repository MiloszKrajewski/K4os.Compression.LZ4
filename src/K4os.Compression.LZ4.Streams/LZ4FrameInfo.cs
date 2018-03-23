namespace K4os.Compression.LZ4.Streams {
	internal class LZ4FrameInfo: ILZ4FrameInfo
	{
		public bool Chaining { get; }
		public bool Checksum { get; }
		public uint? Dictionary { get; }
		public int BlockSize { get; }

		public LZ4FrameInfo(bool chaining, bool checksum, uint? dictionary, int blockSize)
		{
			Chaining = chaining;
			Checksum = checksum;
			Dictionary = dictionary;
			BlockSize = blockSize;
		}
	}
}