namespace K4os.Compression.LZ4.Streams
{
	/// <summary>
	/// LZ4 frame descriptor.
	/// </summary>
	public class LZ4Descriptor: ILZ4Descriptor
	{
		/// <summary>Content length (if available).</summary>
		public long? ContentLength { get; }

		/// <summary>Indicates if content checksum if present.</summary>
		public bool ContentChecksum { get; }

		/// <summary>Indicates if blocks are chained.</summary>
		public bool Chaining { get; }

		/// <summary>Indicates if block checksums are present.</summary>
		public bool BlockChecksum { get; }

		/// <summary>Dictionary id (or null).</summary>
		public uint? Dictionary { get; }

		/// <summary>Block size.</summary>
		public int BlockSize { get; }

		/// <summary>Creates new instance of <see cref="LZ4Descriptor"/>.</summary>
		/// <param name="contentLength">Content length.</param>
		/// <param name="contentChecksum">Content checksum flag.</param>
		/// <param name="chaining">Chaining flag.</param>
		/// <param name="blockChecksum">Block checksum flag.</param>
		/// <param name="dictionary">Dictionary id.</param>
		/// <param name="blockSize">Block size.</param>
		public LZ4Descriptor(
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
