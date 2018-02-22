namespace K4os.Compression.LZ4.Encoders
{
	public static class LZ4StreamEncoderExtensions
	{
		public static unsafe int Topup(
			this ILZ4StreamEncoder encoder, byte[] source, int index, int length)
		{
			fixed (byte* sourceP = source)
				return encoder.Topup(sourceP + index, length);
		}

		public static unsafe int Encode(
			this ILZ4StreamEncoder encoder, byte[] target, int index, int length)
		{
			fixed (byte* targetP = target)
				return encoder.Encode(targetP + index, length);
		}
	}
}
