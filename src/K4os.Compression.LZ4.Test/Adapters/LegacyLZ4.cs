namespace K4os.Compression.LZ4.Test.Adapters
{
	using LZ4Codec = global::LZ4.LZ4Codec;

	public class LegacyLZ4
	{
		public static byte[] Encode(byte[] source, int sourceOffset, int sourceLength) =>
			LZ4Codec.Encode(source, sourceOffset, sourceLength);

		public static byte[] Decode(byte[] source, int sourceOffset, int sourceLength, int targetLength) =>
			LZ4Codec.Decode(source, sourceOffset, sourceLength, targetLength);
	}
}
