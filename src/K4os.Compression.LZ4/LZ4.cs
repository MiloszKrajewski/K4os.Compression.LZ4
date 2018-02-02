using System;
using System.Collections.Generic;
using System.Text;

namespace K4os.Compression.LZ4
{
	public class LZ4Interface
	{
		public static unsafe int Encode(byte* source, byte* target, int sourceLength, int targetLength)
		{
			return LZ4_64.LZ4_compress_default(source, target, sourceLength, targetLength);
		}

		public static unsafe int Encode(byte[] source, byte[] target)
		{
			fixed (byte* sourceP = source)
			fixed (byte* targetP = target)
				return LZ4_64.LZ4_compress_default(sourceP, targetP, source.Length, target.Length);
		}

		public static int MaximumOutputSize(int length) => LZ4_xx.LZ4_compressBound(length);

		public static unsafe int Decode(byte* source, byte* target, int sourceLength, int targetLength) =>
			LZ4_64.LZ4_decompress_safe(source, target, sourceLength, targetLength);

		public static unsafe int Decode(byte[] source, byte[] target, int sourceLength)
		{
			fixed (byte* sourceP = source)
			fixed (byte* targetP = target)
				return LZ4_64.LZ4_decompress_safe(sourceP, targetP, sourceLength, target.Length);
		}
	}
}
