namespace K4os.Compression.LZ4.Streams
{
	public class LZ4FrameInfo: ILZ4FrameInfo
	{
		public long? ContentLength { get; }
		public bool ContentChecksum { get; }
		public bool Chaining { get; }
		public bool BlockChecksum { get; }
		public int BlockSize { get; }

		public LZ4FrameInfo(
			long? contentLength, bool contentChecksum,
			bool chaining, bool blockChecksum,
			int blockSize)
		{
			ContentLength = contentLength;
			ContentChecksum = contentChecksum;
			Chaining = chaining;
			BlockChecksum = blockChecksum;
			BlockSize = blockSize;
		}
	}
}
