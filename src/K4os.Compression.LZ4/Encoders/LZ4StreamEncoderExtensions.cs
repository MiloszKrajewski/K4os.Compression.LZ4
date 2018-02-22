namespace K4os.Compression.LZ4.Encoders
{
	public static class LZ4StreamEncoderExtensions
	{
		public static unsafe bool Topup(
			this ILZ4StreamEncoder encoder, ref byte* source, int length)
		{
			var loaded = encoder.Topup(source, length);
			source += loaded;
			return loaded != 0;
		}

		public static unsafe int Topup(
			this ILZ4StreamEncoder encoder, byte[] source, int index, int length)
		{
			fixed (byte* sourceP = source)
				return encoder.Topup(sourceP + index, length);
		}

		public static bool Topup(
			this ILZ4StreamEncoder encoder, byte[] source, ref int index, int length)
		{
			var loaded = encoder.Topup(source, index, length);
			index += loaded;
			return loaded != 0;
		}

		public static unsafe int Encode(
			this ILZ4StreamEncoder encoder, byte[] target, int index, int length)
		{
			fixed (byte* targetP = target)
				return encoder.Encode(targetP + index, length);
		}

		public static bool Encode(
			this ILZ4StreamEncoder encoder, byte[] target, ref int index, int length)
		{
			var encoded = encoder.Encode(target, index, length);
			index += encoded;
			return encoded != 0;
		}

		public static unsafe bool Encode(
			this ILZ4StreamEncoder encoder, ref byte* target, int length)
		{
			var encoded = encoder.Encode(target, length);
			target += encoded;
			return encoded != 0;
		}

		private static unsafe bool TopupAndEncode(
			ILZ4StreamEncoder encoder,
			byte* source, int sourceLength,
			byte* target, int targetLength,
			bool force,
			out int loaded, out int encoded)
		{
			loaded = 0;
			encoded = 0;

			if (sourceLength <= 0)
				return false;

			loaded = encoder.Topup(source, sourceLength);

			var blockSize = encoder.BlockSize;
			var bytesReady = encoder.BytesReady;
			if (bytesReady >= blockSize || force && bytesReady > 0)
				encoded = encoder.Encode(target, targetLength);

			return loaded != 0 || encoded != 0;
		}

		public static unsafe bool TopupAndEncode(
			this ILZ4StreamEncoder encoder,
			ref byte* source, int sourceLength,
			ref byte* target, int targetLength,
			bool force = false)
		{
			var success = TopupAndEncode(
				encoder,
				source,
				sourceLength,
				target,
				targetLength,
				force,
				out var loaded,
				out var encoded);
			source += loaded;
			target += encoded;
			return success;
		}

		public static unsafe bool TopupAndEncode(
			this ILZ4StreamEncoder encoder,
			byte[] source, ref int sourceIndex, int sourceLength,
			byte[] target, ref int targetIndex, int targetLength,
			bool force = false)
		{
			fixed (byte* sourceP = source)
			fixed (byte* targetP = target)
			{
				var success = TopupAndEncode(
					encoder,
					sourceP + sourceIndex,
					sourceLength,
					targetP + targetIndex,
					targetLength,
					force,
					out var loaded,
					out var encoded);
				sourceIndex += loaded;
				targetIndex += encoded;
				return success;
			}
		}
	}
}
