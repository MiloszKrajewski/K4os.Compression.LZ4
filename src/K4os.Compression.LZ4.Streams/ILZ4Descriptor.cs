namespace K4os.Compression.LZ4.Streams
{
	/// <summary>
	/// LZ4 Frame descriptor.
	/// </summary>
	public interface ILZ4Descriptor
	{
		/// <summary>Content length. Not always known.</summary>
		long? ContentLength { get; }

		/// <summary>Indicates if content checksum is provided.</summary>
		bool ContentChecksum { get; }

		/// <summary>Indicates if blocks are chained (dependent) or not (independent).</summary>
		bool Chaining { get; }

		/// <summary>Indicates if block checksums are provided.</summary>
		bool BlockChecksum { get; }

		/// <summary>Dictionary id. May be null.</summary>
		uint? Dictionary { get; }

		/// <summary>Block size.</summary>
		int BlockSize { get; }
	}
}
