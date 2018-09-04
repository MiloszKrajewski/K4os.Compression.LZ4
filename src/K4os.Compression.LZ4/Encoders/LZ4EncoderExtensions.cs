namespace K4os.Compression.LZ4.Encoders
{
	public static class LZ4EncoderExtensions
	{
		public static unsafe bool Topup(
			this ILZ4Encoder encoder, ref byte* source, int length)
		{
			var loaded = encoder.Topup(source, length);
			source += loaded;
			return loaded != 0;
		}

		public static unsafe int Topup(
			this ILZ4Encoder encoder, byte[] source, int index, int length)
		{
			fixed (byte* sourceP = source)
				return encoder.Topup(sourceP + index, length);
		}

		public static bool Topup(
			this ILZ4Encoder encoder, byte[] source, ref int index, int length)
		{
			var loaded = encoder.Topup(source, index, length);
			index += loaded;
			return loaded != 0;
		}

		public static unsafe int Encode(
			this ILZ4Encoder encoder, byte[] target, int index, int length)
		{
			fixed (byte* targetP = target)
				return encoder.Encode(targetP + index, length);
		}

		public static unsafe int Copy(
			this ILZ4Encoder encoder, byte[] target, int index, int length)
		{
			fixed (byte* targetP = target)
				return encoder.Copy(targetP + index, length);
		}

		public static bool Encode(
			this ILZ4Encoder encoder, byte[] target, ref int index, int length)
		{
			var encoded = encoder.Encode(target, index, length);
			index += encoded;
			return encoded != 0;
		}

		public static unsafe bool Encode(
			this ILZ4Encoder encoder, ref byte* target, int length)
		{
			var encoded = encoder.Encode(target, length);
			target += encoded;
			return encoded != 0;
		}

		public static unsafe EncoderAction TopupAndEncode(
			this ILZ4Encoder encoder,
			byte* source, int sourceLength,
			byte* target, int targetLength,
			bool force, bool allowCopy,
			out int loaded, out int encoded)
		{
			loaded = 0;
			encoded = 0;

			if (sourceLength > 0)
				loaded = encoder.Topup(source, sourceLength);

			return encoder.FlushAndEncode(
				target, targetLength, force, allowCopy, loaded, out encoded);
		}

		public static unsafe EncoderAction TopupAndEncode(
			this ILZ4Encoder encoder,
			byte[] source, int sourceIndex, int sourceLength,
			byte[] target, int targetIndex, int targetLength,
			bool force, bool allowCopy,
			out int loaded, out int encoded)
		{
			fixed (byte* sourceP = source)
			fixed (byte* targetP = target)
				return encoder.TopupAndEncode(
					sourceP + sourceIndex, sourceLength,
					targetP + targetIndex, targetLength,
					force, allowCopy,
					out loaded, out encoded);
		}

		private static unsafe EncoderAction FlushAndEncode(
			this ILZ4Encoder encoder,
			byte* target, int targetLength,
			bool force, bool allowCopy,
			int loaded, out int encoded)
		{
			encoded = 0;

			var blockSize = encoder.BlockSize;
			var bytesReady = encoder.BytesReady;

			if (bytesReady < (force ? 1 : blockSize))
				return loaded > 0 ? EncoderAction.Loaded : EncoderAction.None;

			encoded = encoder.Encode(target, targetLength);
			if (allowCopy && encoded >= bytesReady)
			{
				encoded = encoder.Copy(target, targetLength);
				return EncoderAction.Copied;
			}
			else
			{
				return EncoderAction.Encoded;
			}
		}

		public static unsafe EncoderAction FlushAndEncode(
			this ILZ4Encoder encoder,
			byte* target, int targetLength,
			bool allowCopy,
			out int encoded) =>
			encoder.FlushAndEncode(target, targetLength, true, allowCopy, 0, out encoded);

		public static unsafe EncoderAction FlushAndEncode(
			this ILZ4Encoder encoder,
			byte[] target, int targetIndex, int targetLength,
			bool allowCopy,
			out int encoded)
		{
			fixed (byte* targetP = target)
				return encoder.FlushAndEncode(
					targetP + targetIndex, targetLength, true, allowCopy, 0, out encoded);
		}

		public static unsafe void Drain(
			this ILZ4Decoder decoder,
			byte[] target, int targetIndex,
			int offset, int length)
		{
			fixed (byte* targetP = target)
				decoder.Drain(targetP + targetIndex, offset, length);
		}

		public static unsafe bool DecodeAndDrain(
			this ILZ4Decoder decoder,
			byte* source, int sourceLength,
			byte* target, int targetLength,
			out int decoded)
		{
			decoded = 0;

			if (sourceLength <= 0)
				return false;

			decoded = decoder.Decode(source, sourceLength);
			if (decoded <= 0 || targetLength < decoded)
				return false;

			decoder.Drain(target, -decoded, decoded);

			return true;
		}

		public static unsafe bool DecodeAndDrain(
			this ILZ4Decoder decoder,
			byte[] source, int sourceIndex, int sourceLength,
			byte[] target, int targetIndex, int targetLength,
			out int decoded)
		{
			fixed (byte* sourceP = source)
			fixed (byte* targetP = target)
				return decoder.DecodeAndDrain(
					sourceP + sourceIndex,
					sourceLength,
					targetP + targetIndex,
					targetLength,
					out decoded);
		}
	}
}
