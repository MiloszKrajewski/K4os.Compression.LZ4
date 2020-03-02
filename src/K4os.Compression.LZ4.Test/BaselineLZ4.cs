using System;
using K4os.Compression.LZ4.Test.Baseline;

namespace K4os.Compression.LZ4.Test
{
	public class BaselineLZ4
	{
		public static unsafe byte[] Encode(
			byte[] source, int sourceOffset, int sourceLength)
		{
			var targetLength = LZ4_xx.LZ4_compressBound(sourceLength);
			var target = new byte[targetLength];

			fixed (byte* sourceP = &source[sourceOffset])
			fixed (byte* targetP = target)
			{
				var encodedLength = LZ4_64.LZ4_compress_default(
					sourceP, targetP, sourceLength, targetLength);
				if (encodedLength <= 0)
					throw new ArgumentException();
				
				var encoded = new byte[encodedLength];
				Buffer.BlockCopy(target, 0, encoded, 0, encodedLength);
				return encoded;
			}
		}

		public static unsafe byte[] Decode(
			byte[] source, int sourceOffset, int sourceLength, int targetLength)
		{
			var target = new byte[targetLength];

			fixed (byte* sourceP = &source[sourceOffset])
			fixed (byte* targetP = target)
			{
				var decodedLength = LZ4_xx.LZ4_decompress_safe(
					sourceP, targetP, sourceLength, targetLength);
				if (decodedLength != targetLength)
					throw new ArgumentException();
			}

			return target;
		}
	}
}
