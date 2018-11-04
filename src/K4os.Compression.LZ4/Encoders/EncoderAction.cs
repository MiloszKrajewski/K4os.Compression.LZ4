namespace K4os.Compression.LZ4.Encoders
{
	/// <summary>
	/// Action performed by encoder using <c>FlushAndEncode</c> method.
	/// </summary>
	public enum EncoderAction
	{
		/// <summary>Nothing has happened, most likely loading 0 bytes.</summary>
		None,
		/// <summary>
		/// Some bytes has been loaded
		/// </summary>
		Loaded,
		Copied,
		Encoded,
	}
}
