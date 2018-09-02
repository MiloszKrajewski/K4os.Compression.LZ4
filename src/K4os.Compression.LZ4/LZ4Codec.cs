using System;
using K4os.Compression.LZ4.Internal;

namespace K4os.Compression.LZ4
{
	public class LZ4Codec
	{
		public static int MaximumOutputSize(int length) => LZ4_xx.LZ4_compressBound(length);

		private static void Validate(byte[] buffer, int index, int length)
		{
			if (buffer == null)
				throw new ArgumentNullException(
					nameof(buffer), "cannot be null");

			var valid = index >= 0 && length >= 0 && index + length <= buffer.Length;
			if (!valid)
				throw new ArgumentException(
					$"invalid index/length combination: {index}/{length}");
		}

		public static unsafe int Encode(
			byte* source, byte* target,
			int sourceLength, int targetLength,
			LZ4Level level) =>
			level == LZ4Level.L00_FAST
				? LZ4_64.LZ4_compress_default(source, target, sourceLength, targetLength)
				: LZ4_64_HC.LZ4_compress_HC(
					source, target, sourceLength, targetLength, (int) level);

		public static unsafe int Encode(
			byte[] source, int sourceIndex, int sourceLength,
			byte[] target, int targetIndex, int targetLength,
			LZ4Level level)
		{
			Validate(source, sourceIndex, sourceLength);
			Validate(target, targetIndex, targetLength);
			fixed (byte* sourceP = &source[sourceIndex])
			fixed (byte* targetP = &target[targetIndex])
				return Encode(
					sourceP, targetP, sourceLength, targetLength,
					level);
		}

		public static byte[] Encode(
			byte[] source, int sourceIndex, int sourceLength, LZ4Level level)
		{
			Validate(source, sourceIndex, sourceLength);

			var bufferLength = MaximumOutputSize(sourceLength);
			var buffer = new byte[bufferLength];
			var targetLength = Encode(
				source, sourceIndex, sourceLength, buffer, 0, bufferLength, level);
			if (targetLength == bufferLength)
				return buffer;

			var target = new byte[targetLength];
			Buffer.BlockCopy(buffer, 0, target, 0, targetLength);
			return target;
		}

		public static unsafe int Decode(
			byte* source, byte* target, int sourceLength, int targetLength) =>
			LZ4_xx.LZ4_decompress_safe(source, target, sourceLength, targetLength);

		public static unsafe int Decode(
			byte[] source, int sourceIndex, int sourceLength,
			byte[] target, int targetIndex, int targetLength)
		{
			Validate(source, sourceIndex, sourceLength);
			Validate(target, targetIndex, targetLength);
			fixed (byte* sourceP = &source[sourceIndex])
			fixed (byte* targetP = &target[targetIndex])
				return Decode(sourceP, targetP, sourceLength, targetLength);
		}

		public static byte[] Decode(
			byte[] source, int sourceIndex, int sourceLength, int targetLength)
		{
			Validate(source, sourceIndex, sourceLength);

			var result = new byte[targetLength];
			var decodedLength = Decode(source, sourceIndex, sourceLength, result, 0, targetLength);
			if (decodedLength != targetLength)
				throw new ArgumentException(
					$"Decoded length does not match expected value: {decodedLength}/{targetLength}");

			return result;
		}

		public static unsafe byte[] Pickle(
			byte[] source, int sourceIndex, int sourceLength,
			LZ4Level level = LZ4Level.L00_FAST)
		{
			if (sourceLength <= 0)
				return BitConverter.GetBytes((uint) 0);

			Validate(source, sourceIndex, sourceLength);

			var targetP = (byte*) Mem.Alloc(sourceLength);
			try
			{
				fixed (byte* sourceP = source)
				{
					var encodedLength = Encode(
						sourceP, targetP, sourceLength, sourceLength, level);

					return encodedLength <= 0
						? PickleCopy(sourceP, sourceLength, sourceLength)
						: PickleCopy(targetP, encodedLength, sourceLength);
				}
			}
			finally
			{
				Mem.Free(targetP);
			}
		}

		private static unsafe byte[] PickleCopy(
			byte* target, int targetLength, int sourceLength)
		{
			var result = new byte[sizeof(int) + targetLength];
			fixed (byte* resultP = result)
			{
				Mem.Poke32(resultP, (uint) sourceLength);
				Mem.Move(resultP + sizeof(int), target, targetLength);
			}

			return result;
		}

		public static byte[] Pickle(byte[] source, LZ4Level level = LZ4Level.L00_FAST) =>
			Pickle(source, 0, source.Length);

		public static byte[] Unpickle(
			byte[] source, int sourceIndex, int sourceLength)
		{
			var targetLength = BitConverter.ToInt32(source, sourceIndex);
			if (targetLength <= 0)
				return Array.Empty<byte>();

			sourceIndex += sizeof(int);
			sourceLength -= sizeof(int);

			var target = new byte[targetLength];

			if (targetLength == sourceLength)
			{
				Buffer.BlockCopy(source, sourceIndex, target, 0, targetLength);
			}
			else
			{
				var decodedLength = Decode(
					source, sourceIndex, sourceLength, target, 0, targetLength);
				if (decodedLength != targetLength)
					throw new ArgumentException(
						$"Decoded length does not match expected value: {decodedLength}/{targetLength}");
			}

			return target;
		}

		public static byte[] Unpickle(byte[] source) =>
			Unpickle(source, 0, source.Length);
	}
}
