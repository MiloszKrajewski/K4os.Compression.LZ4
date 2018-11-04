using K4os.Compression.LZ4.Internal;

namespace K4os.Compression.LZ4.Streams
{
	/// <summary>
	/// LZ4 encoder settings.
	/// </summary>
	public class LZ4EncoderSettings
	{
		internal static LZ4EncoderSettings Default = new LZ4EncoderSettings();

		/// <summary>
		/// Content length. It is not enforced, it can be set to any value, but it will be
		/// written to the stream so it can be used while decoding. If you don't know the length
		/// just leave default value.
		/// </summary>
		public long? ContentLength { get; set; } = null;
		
		/// <summary>
		/// Indicates if blocks should be chained (dependent) or not (independent). Dependent blocks
		/// (with chaining) provide better compression ratio but are a little but slower and take
		/// more memory. 
		/// </summary>
		public bool ChainBlocks { get; set; } = true;
		
		/// <summary>
		/// Block size. You can use any block size, but default values for LZ4 are 64k, 256k, 1m,
		/// and 4m. 64k is good enough for dependent blocks, but for independent blocks bigger is
		/// better. 
		/// </summary>
		public int BlockSize { get; set; } = Mem.K64;
		
		/// <summary>Indicates is content checksum is provided. Not implemented yet.</summary>
		public bool ContentChecksum => false;
		
		/// <summary>Indicates if block checksum is provided. Not implemented yet.</summary>
		public bool BlockChecksum => false;
		
		/// <summary>Dictionary id. Not implemented yet.</summary>
		public uint? Dictionary => null;
		
		/// <summary>Compression level.</summary>
		public LZ4Level CompressionLevel { get; set; } = LZ4Level.L00_FAST;
		
		/// <summary>Extra memory (for the process, more is usually better).</summary>
		public int ExtraMemory { get; set; }
	}
}
