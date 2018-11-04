namespace K4os.Compression.LZ4.Streams
{
	/// <summary>
	/// Decoder settings.
	/// </summary>
	public class LZ4DecoderSettings
	{
		internal static LZ4DecoderSettings Default = new LZ4DecoderSettings();

		/// <summary>Extra memory for decompression.</summary>
		public int ExtraMemory { get; set; }
	}
}
