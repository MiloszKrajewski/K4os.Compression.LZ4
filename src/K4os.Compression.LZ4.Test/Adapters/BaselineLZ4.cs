using System;

namespace K4os.Compression.LZ4.Test.Adapters
{
	using LZ4Level = K4os.Compression.LZ4.vPrev.LZ4Level;
	using LZ4Codec = K4os.Compression.LZ4.vPrev.LZ4Codec;

	public class BaselineLZ4
	{
		public const int ExpectedVersion = 181;

		public static byte[] Encode(
			byte[] source, int sourceIndex, int sourceLength, LZ4Level level = LZ4Level.L00_FAST)
		{
			var bufferLength = LZ4Codec.MaximumOutputSize(sourceLength);
			var buffer = new byte[bufferLength];
			var targetLength = LZ4Codec.Encode(
				source, sourceIndex, sourceLength, buffer, 0, bufferLength, level);
			if (targetLength <= 0)
				throw new ArgumentException("Encoding failed. No bytes returned.");
			
			if (targetLength == bufferLength)
				return buffer;

			var target = new byte[targetLength];
			Buffer.BlockCopy(buffer, 0, target, 0, targetLength);
			return target;
		}

		public static byte[] Decode(
			byte[] source, int sourceIndex, int sourceLength, int targetLength)
		{
			var result = new byte[targetLength];
			var decodedLength = LZ4Codec.Decode(
				source, sourceIndex, sourceLength, result, 0, targetLength);
			if (decodedLength != targetLength)
				throw new ArgumentException(
					$"Decoded length does not match expected value: {decodedLength}/{targetLength}");

			return result;
		}
	}
}
