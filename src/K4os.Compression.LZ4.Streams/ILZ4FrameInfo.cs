namespace K4os.Compression.LZ4.Streams
{
	public interface ILZ4FrameInfo
	{
		bool Chaining { get; }
		bool Checksum { get; }

		uint? Dictionary { get; }
		int BlockSize { get; }
	}
}
