using System;

namespace K4os.Compression.LZ4
{
	// ReSharper disable once InconsistentNaming
	public class LZ4Codec
	{
		public static int MaximumOutputSize(int length) => LZ4_xx.LZ4_compressBound(length);

		private static void Validate(byte[] buffer, int index, int length)
		{
			if (buffer == null) throw new ArgumentNullException(nameof(buffer), "cannot be null");

			var valid = index >= 0 && length >= 0 && index + length <= buffer.Length;
			if (!valid) throw new ArgumentException($"invald index/length combination: {index}/{length}");
		}

		public static unsafe int Encode64(
			byte* source, byte* target, int sourceLength, int targetLength) =>
			LZ4_64.LZ4_compress_default(source, target, sourceLength, targetLength);

		public static unsafe int EncodeHC(
			byte* source, byte* target, int sourceLength, int targetLength, int level = 0) =>
			LZ4_64_HC.LZ4_compress_HC(source, target, sourceLength, targetLength, level);

		public static unsafe int Encode64(
			byte[] source, int sourceIndex, int sourceLength,
			byte[] target, int targetIndex, int targetLength)
		{
			Validate(source, sourceIndex, sourceLength);
			Validate(target, targetIndex, targetLength);
			fixed (byte* sourceP = source)
			fixed (byte* targetP = target)
				return Encode64(sourceP + sourceIndex, targetP + targetIndex, sourceLength, targetLength);
		}

		public static unsafe int EncodeHC(
			byte[] source, int sourceIndex, int sourceLength,
			byte[] target, int targetIndex, int targetLength,
			int level = 0)
		{
			Validate(source, sourceIndex, sourceLength);
			Validate(target, targetIndex, targetLength);
			fixed (byte* sourceP = source)
			fixed (byte* targetP = target)
				return EncodeHC(
					sourceP + sourceIndex,
					targetP + targetIndex,
					sourceLength,
					targetLength,
					level);
		}

		public static byte[] Encode64(byte[] source, int sourceIndex, int sourceLength)
		{
			Validate(source, sourceIndex, sourceLength);

			var bufferLength = MaximumOutputSize(sourceLength);
			var buffer = new byte[bufferLength];
			var targetLength = Encode64(source, sourceIndex, sourceLength, buffer, 0, bufferLength);
			if (targetLength == bufferLength)
				return buffer;

			var target = new byte[targetLength];
			Buffer.BlockCopy(buffer, 0, target, 0, targetLength);
			return target;
		}

		public static byte[] EncodeHC(byte[] source, int sourceIndex, int sourceLength, int level)
		{
			Validate(source, sourceIndex, sourceLength);

			var bufferLength = MaximumOutputSize(sourceLength);
			var buffer = new byte[bufferLength];
			var targetLength = EncodeHC(source, sourceIndex, sourceLength, buffer, 0, bufferLength, level);
			if (targetLength == bufferLength)
				return buffer;

			var target = new byte[targetLength];
			Buffer.BlockCopy(buffer, 0, target, 0, targetLength);
			return target;
		}

		public static unsafe int Decode(byte* source, byte* target, int sourceLength, int targetLength) =>
			LZ4_xx.LZ4_decompress_safe(source, target, sourceLength, targetLength);

		public static unsafe int Decode(
			byte[] source, int sourceIndex, int sourceLength,
			byte[] target, int targetIndex, int targetLength)
		{
			Validate(source, sourceIndex, sourceLength);
			Validate(target, targetIndex, targetLength);
			fixed (byte* sourceP = source)
			fixed (byte* targetP = target)
				return Decode(sourceP, targetP, sourceLength, target.Length);
		}

		public static byte[] Decode(byte[] source, int sourceIndex, int sourceLength, int targetLength)
		{
			Validate(source, sourceIndex, sourceLength);

			var result = new byte[targetLength];
			var decodedLength = Decode(source, sourceIndex, sourceLength, result, 0, targetLength);
			if (decodedLength != targetLength)
				throw new ArgumentException(
					$"Decoded length does not match expected value: {decodedLength}/{targetLength}");

			return result;
		}
	}
}
