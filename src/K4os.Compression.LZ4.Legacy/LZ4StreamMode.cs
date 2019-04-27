namespace K4os.Compression.LZ4.Legacy
{
	/// <summary>
	/// Originally this type comes from System.IO.Compression, 
	/// but it is not present in portable assemblies.
	/// </summary>
	public enum LZ4StreamMode
	{
		/// <summary>Compress</summary>
		Compress,

		/// <summary>Decompress</summary>
		Decompress,
	}
}