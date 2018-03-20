namespace K4os.Compression.LZ4.Streams
{
	public class LZ4FrameInfo
	{
		public bool Chaining { get; }
		public bool Checksum { get; }

		public uint? Dictionary { get; }
		public int BlockSize { get; }
	}
}
