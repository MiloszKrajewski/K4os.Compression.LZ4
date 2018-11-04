using System;

namespace K4os.Compression.LZ4.Encoders
{
	/// <summary>
	/// Functionality of encoders added on top of fixed interface.
	/// </summary>
	public static class LZ4EncoderExtensions
	{
		/// <summary>Tops encoder up with some data.</summary>
		/// <param name="encoder">Encoder.</param>
		/// <param name="source">Buffer pointer, will be shifted after operation by the number of
		/// bytes actually loaded.</param>
		/// <param name="length">Length of buffer.</param>
		/// <returns><c>true</c> if buffer was topped up, <c>false</c> if no bytes were loaded.</returns>
		public static unsafe bool Topup(
			this ILZ4Encoder encoder, ref byte* source, int length)
		{
			var loaded = encoder.Topup(source, length);
			source += loaded;
			return loaded != 0;
		}

		/// <summary>Tops encoder up with some data.</summary>
		/// <param name="encoder">Encoder.</param>
		/// <param name="source">Buffer.</param>
		/// <param name="offset">Buffer offset.</param>
		/// <param name="length">Length of buffer.</param>
		/// <returns>Number of bytes actually loaded.</returns>
		public static unsafe int Topup(
			this ILZ4Encoder encoder, byte[] source, int offset, int length)
		{
			fixed (byte* sourceP = source)
				return encoder.Topup(sourceP + offset, length);
		}

		/// <summary>Tops encoder up with some data.</summary>
		/// <param name="encoder">Encoder.</param>
		/// <param name="source">Buffer.</param>
		/// <param name="offset">Buffer offset, will be increased after operation by the number
		/// of bytes actually loaded.</param>
		/// <param name="length">Length of buffer.</param>
		/// <returns><c>true</c> if buffer was topped up, <c>false</c> if no bytes were loaded.</returns>
		public static bool Topup(
			this ILZ4Encoder encoder, byte[] source, ref int offset, int length)
		{
			var loaded = encoder.Topup(source, offset, length);
			offset += loaded;
			return loaded != 0;
		}

		/// <summary>Encodes all bytes currently stored in encoder into target buffer.</summary>
		/// <param name="encoder">Encoder.</param>
		/// <param name="target">Target buffer.</param>
		/// <param name="offset">Offset in target buffer.</param>
		/// <param name="length">Length of target buffer.</param>
		/// <param name="allowCopy">if <c>true</c> copying bytes is allowed.</param>
		/// <returns>Number of bytes encoder. If bytes were copied than this value is negative.</returns>
		public static unsafe int Encode(
			this ILZ4Encoder encoder, byte[] target, int offset, int length, bool allowCopy)
		{
			fixed (byte* targetP = target)
				return encoder.Encode(targetP + offset, length, allowCopy);
		}

		/// <summary>Encodes all bytes currently stored in encoder into target buffer.</summary>
		/// <param name="encoder">Encoder.</param>
		/// <param name="target">Target buffer.</param>
		/// <param name="offset">Offset in target buffer. Will be updated after operation.</param>
		/// <param name="length">Length of target buffer.</param>
		/// <param name="allowCopy">if <c>true</c> copying bytes is allowed.</param>
		/// <returns>Result of this action. Bytes can be Copied (<see cref="EncoderAction.Copied"/>),
		/// Encoded (<see cref="EncoderAction.Encoded"/>) or nothing could have
		/// happened (<see cref="EncoderAction.None"/>).</returns>
		public static EncoderAction Encode(
			this ILZ4Encoder encoder, byte[] target, ref int offset, int length, bool allowCopy)
		{
			var encoded = encoder.Encode(target, offset, length, allowCopy);
			offset += Math.Abs(encoded);
			return 
				encoded == 0 ? EncoderAction.None : 
				encoded < 0 ? EncoderAction.Copied : 
				EncoderAction.Encoded;
		}

		/// <summary>Encodes all bytes currently stored in encoder into target buffer.</summary>
		/// <param name="encoder">Encoder.</param>
		/// <param name="target">Target buffer. Will be updated after operation.</param>
		/// <param name="length">Length of buffer.</param>
		/// <param name="allowCopy">if <c>true</c> copying bytes is allowed.</param>
		/// <returns>Result of this action. Bytes can be Copied (<see cref="EncoderAction.Copied"/>),
		/// Encoded (<see cref="EncoderAction.Encoded"/>) or nothing could have
		/// happened (<see cref="EncoderAction.None"/>).</returns>
		public static unsafe EncoderAction Encode(
			this ILZ4Encoder encoder, ref byte* target, int length, bool allowCopy)
		{
			var encoded = encoder.Encode(target, length, allowCopy);
			target += Math.Abs(encoded);
			return 
				encoded == 0 ? EncoderAction.None : 
				encoded < 0 ? EncoderAction.Copied : 
				EncoderAction.Encoded;
		}

		public static unsafe EncoderAction TopupAndEncode(
			this ILZ4Encoder encoder,
			byte* source, int sourceLength,
			byte* target, int targetLength,
			bool forceEncode, bool allowCopy,
			out int loaded, out int encoded)
		{
			loaded = 0;
			encoded = 0;

			if (sourceLength > 0)
				loaded = encoder.Topup(source, sourceLength);

			return encoder.FlushAndEncode(
				target, targetLength, forceEncode, allowCopy, loaded, out encoded);
		}

		public static unsafe EncoderAction TopupAndEncode(
			this ILZ4Encoder encoder,
			byte[] source, int sourceIndex, int sourceLength,
			byte[] target, int targetIndex, int targetLength,
			bool forceEncode, bool allowCopy,
			out int loaded, out int encoded)
		{
			fixed (byte* sourceP = source)
			fixed (byte* targetP = target)
				return encoder.TopupAndEncode(
					sourceP + sourceIndex, sourceLength,
					targetP + targetIndex, targetLength,
					forceEncode, allowCopy,
					out loaded, out encoded);
		}

		private static unsafe EncoderAction FlushAndEncode(
			this ILZ4Encoder encoder,
			byte* target, int targetLength,
			bool forceEncode, bool allowCopy,
			int loaded, out int encoded)
		{
			encoded = 0;

			var blockSize = encoder.BlockSize;
			var bytesReady = encoder.BytesReady;

			if (bytesReady < (forceEncode ? 1 : blockSize))
				return loaded > 0 ? EncoderAction.Loaded : EncoderAction.None;

			encoded = encoder.Encode(target, targetLength, allowCopy);
			if (allowCopy && encoded < 0)
			{
				encoded = -encoded;
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
