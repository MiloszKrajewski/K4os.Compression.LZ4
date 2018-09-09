namespace K4os.Compression.LZ4.Streams
{
	public class LZ4DecoderSettings
	{
		internal static LZ4DecoderSettings Default = new LZ4DecoderSettings();

		public int ExtraMemory { get; set; } = 0;
	}
}
