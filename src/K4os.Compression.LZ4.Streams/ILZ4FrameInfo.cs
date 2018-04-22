namespace K4os.Compression.LZ4.Streams
{
	public interface ILZ4FrameInfo
	{
		bool ContentChecksum { get; }

		bool Chaining { get; }
		bool BlockChecksum { get; }

		uint? Dictionary { get; }
		int BlockSize { get; }
	}
}
