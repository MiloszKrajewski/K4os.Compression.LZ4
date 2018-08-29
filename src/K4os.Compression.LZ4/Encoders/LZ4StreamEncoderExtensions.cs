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

		public static unsafe int Copy(
			this ILZ4StreamEncoder encoder, byte[] target, int index, int length)
		{
			fixed (byte* targetP = target)
				return encoder.Copy(targetP + index, length);
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

		public static unsafe bool TopupAndEncode(
			this ILZ4StreamEncoder encoder,
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
			{
				#error there is no way to indicate failed compression 
				if ((encoded = encoder.Encode(target, targetLength)) <= 0) 
					return false;
			}

			return loaded != 0 || encoded != 0;
		}

		public static unsafe bool TopupAndEncode(
			this ILZ4StreamEncoder encoder,
			byte[] source, int sourceIndex, int sourceLength,
			byte[] target, int targetIndex, int targetLength,
			bool force,
			out int loaded, out int encoded)
		{
			fixed (byte* sourceP = source)
			fixed (byte* targetP = target)
				return encoder.TopupAndEncode(
					sourceP + sourceIndex,
					sourceLength,
					targetP + targetIndex,
					targetLength,
					force,
					out loaded,
					out encoded);
		}

		public static unsafe void Drain(
			this ILZ4StreamDecoder decoder,
			byte[] target, int targetIndex,
			int offset, int length)
		{
			fixed (byte* targetP = target)
				decoder.Drain(targetP + targetIndex, offset, length);
		}

		public static unsafe bool DecodeAndDrain(
			this ILZ4StreamDecoder decoder,
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
			this ILZ4StreamDecoder decoder,
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
