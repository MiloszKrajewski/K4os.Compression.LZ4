namespace K4os.Compression.LZ4.Streams
{
	public class LZ4FrameInfo: ILZ4FrameInfo
	{
		public long? ContentLength { get; }
		public bool ContentChecksum { get; }
		public bool Chaining { get; }
		public bool BlockChecksum { get; }
		public uint? Dictionary { get; }
		public int BlockSize { get; }

		public LZ4FrameInfo(
			long? contentLength, bool contentChecksum,
			bool chaining, bool blockChecksum,
			uint? dictionary, int blockSize)
		{
			ContentLength = contentLength;
			ContentChecksum = contentChecksum;
			Chaining = chaining;
			BlockChecksum = blockChecksum;
			Dictionary = dictionary;
			BlockSize = blockSize;
		}
	}
}
