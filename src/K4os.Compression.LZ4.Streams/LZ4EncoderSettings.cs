using K4os.Compression.LZ4.Internal;

namespace K4os.Compression.LZ4.Streams
{
	public class LZ4EncoderSettings
	{
		internal static LZ4EncoderSettings Default = new LZ4EncoderSettings();

		public long? ContentLength { get; set; } = null;
		public bool ChainBlocks { get; set; } = true;
		public int BlockSize { get; set; } = Mem.K64;
		public bool ContentChecksum => false;
		public bool BlockChecksum => false;
		public uint? Dictionary => null;
		public LZ4Level CompressionLevel { get; set; } = LZ4Level.L00_FAST;
		public int ExtraMemory { get; set; } = 0;
	}
}
