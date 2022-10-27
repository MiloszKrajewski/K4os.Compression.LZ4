using System;

namespace K4os.Compression.LZ4.Encoders;

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

	/// <summary>Tops encoder and encodes content.</summary>
	/// <param name="encoder">Encoder.</param>
	/// <param name="source">Source buffer (used to top up from).</param>
	/// <param name="sourceLength">Source buffer length.</param>
	/// <param name="target">Target buffer (used to encode into)</param>
	/// <param name="targetLength">Target buffer length.</param>
	/// <param name="forceEncode">Forces encoding even if encoder is not full.</param>
	/// <param name="allowCopy">Allows to copy bytes if compression was not possible.</param>
	/// <param name="loaded">Number of bytes loaded (topped up)</param>
	/// <param name="encoded">Number if bytes encoded or copied.
	/// Value is 0 if no encoding was done.</param>
	/// <returns>Action performed.</returns>
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

	/// <summary>Tops encoder and encodes content.</summary>
	/// <param name="encoder">Encoder.</param>
	/// <param name="source">Source buffer (used to top up from).</param>
	/// <param name="sourceOffset">Offset within source buffer.</param>
	/// <param name="sourceLength">Source buffer length.</param>
	/// <param name="target">Target buffer (used to encode into)</param>
	/// <param name="targetOffset">Offset within target buffer.</param>
	/// <param name="targetLength">Target buffer length.</param>
	/// <param name="forceEncode">Forces encoding even if encoder is not full.</param>
	/// <param name="allowCopy">Allows to copy bytes if compression was not possible.</param>
	/// <param name="loaded">Number of bytes loaded (topped up)</param>
	/// <param name="encoded">Number if bytes encoded or copied.
	/// Value is 0 if no encoding was done.</param>
	/// <returns>Action performed.</returns>
	public static unsafe EncoderAction TopupAndEncode(
		this ILZ4Encoder encoder,
		byte[] source, int sourceOffset, int sourceLength,
		byte[] target, int targetOffset, int targetLength,
		bool forceEncode, bool allowCopy,
		out int loaded, out int encoded)
	{
		fixed (byte* sourceP = source)
		fixed (byte* targetP = target)
			return encoder.TopupAndEncode(
				sourceP + sourceOffset, sourceLength,
				targetP + targetOffset, targetLength,
				forceEncode, allowCopy,
				out loaded, out encoded);
	}
		
	/// <summary>Tops encoder and encodes content.</summary>
	/// <param name="encoder">Encoder.</param>
	/// <param name="source">Source buffer (used to top up from).</param>
	/// <param name="target">Target buffer (used to encode into)</param>
	/// <param name="forceEncode">Forces encoding even if encoder is not full.</param>
	/// <param name="allowCopy">Allows to copy bytes if compression was not possible.</param>
	/// <param name="loaded">Number of bytes loaded (topped up)</param>
	/// <param name="encoded">Number if bytes encoded or copied.
	/// Value is 0 if no encoding was done.</param>
	/// <returns>Action performed.</returns>
	public static unsafe EncoderAction TopupAndEncode(
		this ILZ4Encoder encoder,
		ReadOnlySpan<byte> source,
		Span<byte> target,
		bool forceEncode, bool allowCopy,
		out int loaded, out int encoded)
	{
		fixed (byte* sourceP = source)
		fixed (byte* targetP = target)
			return encoder.TopupAndEncode(
				sourceP, source.Length,
				targetP, target.Length,
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
		if (!allowCopy || encoded >= 0) 
			return EncoderAction.Encoded;

		encoded = -encoded;
		return EncoderAction.Copied;
	}

	/// <summary>Encoded remaining bytes in encoder.</summary>
	/// <param name="encoder">Encoder.</param>
	/// <param name="target">Target buffer.</param>
	/// <param name="targetLength">Target buffer length.</param>
	/// <param name="allowCopy">Allows to copy bytes if compression was not possible.</param>
	/// <param name="encoded">Number if bytes encoded or copied.
	/// Value is 0 if no encoding was done.</param>
	/// <returns>Action performed.</returns>
	public static unsafe EncoderAction FlushAndEncode(
		this ILZ4Encoder encoder,
		byte* target, int targetLength,
		bool allowCopy,
		out int encoded) =>
		encoder.FlushAndEncode(target, targetLength, true, allowCopy, 0, out encoded);

	/// <summary>Encoded remaining bytes in encoder.</summary>
	/// <param name="encoder">Encoder.</param>
	/// <param name="target">Target buffer.</param>
	/// <param name="targetOffset">Offset within target buffer.</param>
	/// <param name="targetLength">Target buffer length.</param>
	/// <param name="allowCopy">Allows to copy bytes if compression was not possible.</param>
	/// <param name="encoded">Number if bytes encoded or copied.
	/// Value is 0 if no encoding was done.</param>
	/// <returns>Action performed.</returns>
	public static unsafe EncoderAction FlushAndEncode(
		this ILZ4Encoder encoder,
		byte[] target, int targetOffset, int targetLength,
		bool allowCopy,
		out int encoded)
	{
		fixed (byte* targetP = target)
			return encoder.FlushAndEncode(
				targetP + targetOffset, targetLength, 
				true, allowCopy, 0, out encoded);
	}
		
	/// <summary>Encoded remaining bytes in encoder.</summary>
	/// <param name="encoder">Encoder.</param>
	/// <param name="target">Target buffer.</param>
	/// <param name="allowCopy">Allows to copy bytes if compression was not possible.</param>
	/// <param name="encoded">Number if bytes encoded or copied.
	/// Value is 0 if no encoding was done.</param>
	/// <returns>Action performed.</returns>
	public static unsafe EncoderAction FlushAndEncode(
		this ILZ4Encoder encoder,
		Span<byte> target, bool allowCopy, out int encoded)
	{
		fixed (byte* targetP = target)
			return encoder.FlushAndEncode(
				targetP, target.Length, 
				true, allowCopy, 0, out encoded);
	}

	/// <summary>Drains decoder by reading all bytes which are ready.</summary>
	/// <param name="decoder">Decoder.</param>
	/// <param name="target">Target buffer.</param>
	/// <param name="targetOffset">Offset within target buffer.</param>
	/// <param name="offset">Offset in decoder relatively to decoder's head.
	/// Please note, it should be negative value.</param>
	/// <param name="length">Number of bytes.</param>
	public static unsafe void Drain(
		this ILZ4Decoder decoder,
		byte[] target, int targetOffset,
		int offset, int length)
	{
		fixed (byte* targetP = target)
			decoder.Drain(targetP + targetOffset, offset, length);
	}
		
	/// <summary>Drains decoder by reading all bytes which are ready.</summary>
	/// <param name="decoder">Decoder.</param>
	/// <param name="target">Target buffer.</param>
	/// <param name="offset">Offset in decoder relatively to decoder's head.
	/// Please note, it should be negative value.</param>
	/// <param name="length">Number of bytes.</param>
	public static unsafe void Drain(
		this ILZ4Decoder decoder,
		Span<byte> target,
		int offset, int length)
	{
		fixed (byte* targetP = target)
			decoder.Drain(targetP, offset, length);
	}

	/// <summary>Decodes data and immediately drains it into target buffer.</summary>
	/// <param name="decoder">Decoder.</param>
	/// <param name="source">Source buffer (with compressed data, to be decoded).</param>
	/// <param name="sourceLength">Source buffer length.</param>
	/// <param name="target">Target buffer (to drained into).</param>
	/// <param name="targetLength">Target buffer length.</param>
	/// <param name="decoded">Number of bytes actually decoded.</param>
	/// <returns><c>true</c> decoder was drained, <c>false</c> otherwise.</returns>
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

	/// <summary>Decodes data and immediately drains it into target buffer.</summary>
	/// <param name="decoder">Decoder.</param>
	/// <param name="source">Source buffer (with compressed data, to be decoded).</param>
	/// <param name="sourceOffset">Offset within source buffer.</param>
	/// <param name="sourceLength">Source buffer length.</param>
	/// <param name="target">Target buffer (to drained into).</param>
	/// <param name="targetOffset">Offset within target buffer.</param>
	/// <param name="targetLength">Target buffer length.</param>
	/// <param name="decoded">Number of bytes actually decoded.</param>
	/// <returns><c>true</c> decoder was drained, <c>false</c> otherwise.</returns>
	public static unsafe bool DecodeAndDrain(
		this ILZ4Decoder decoder,
		byte[] source, int sourceOffset, int sourceLength,
		byte[] target, int targetOffset, int targetLength,
		out int decoded)
	{
		fixed (byte* sourceP = source)
		fixed (byte* targetP = target)
			return decoder.DecodeAndDrain(
				sourceP + sourceOffset,
				sourceLength,
				targetP + targetOffset,
				targetLength,
				out decoded);
	}
		
	/// <summary>Decodes data and immediately drains it into target buffer.</summary>
	/// <param name="decoder">Decoder.</param>
	/// <param name="source">Source buffer (with compressed data, to be decoded).</param>
	/// <param name="target">Target buffer (to drained into).</param>
	/// <param name="decoded">Number of bytes actually decoded.</param>
	/// <returns><c>true</c> decoder was drained, <c>false</c> otherwise.</returns>
	public static unsafe bool DecodeAndDrain(
		this ILZ4Decoder decoder,
		ReadOnlySpan<byte> source,
		Span<byte> target,
		out int decoded)
	{
		fixed (byte* sourceP = source)
		fixed (byte* targetP = target)
			return decoder.DecodeAndDrain(
				sourceP, source.Length,
				targetP, target.Length,
				out decoded);
	}

	/// <summary>
	/// Inject already decompressed block and caches it in decoder.
	/// Used with uncompressed-yet-chained blocks and pre-made dictionaries.
	/// See <see cref="ILZ4Decoder.Inject"/>.
	/// </summary>
	/// <param name="decoder">Decoder.</param>
	/// <param name="buffer">Uncompressed block.</param>
	/// <param name="offset">Offset in uncompressed block.</param>
	/// <param name="length">Length of uncompressed block.</param>
	/// <returns>Number of decoded bytes.</returns>
	public static unsafe int Inject(
		this ILZ4Decoder decoder, byte[] buffer, int offset, int length)
	{
		fixed (byte* bufferP = buffer)
			return decoder.Inject(bufferP + offset, length);
	}

	/// <summary>
	/// Decodes previously compressed block and caches decompressed block in decoder.
	/// Returns number of bytes decoded.
	/// See <see cref="ILZ4Decoder.Decode"/>.
	/// </summary>
	/// <param name="decoder">Decoder.</param>
	/// <param name="buffer">Compressed block.</param>
	/// <param name="offset">Offset in compressed block.</param>
	/// <param name="length">Length of compressed block.</param>
	/// <param name="blockSize">Size of the block. Value <c>0</c> indicates default block size.</param>
	/// <returns>Number of decoded bytes.</returns>
	public static unsafe int Decode(
		this ILZ4Decoder decoder, byte[] buffer, int offset, int length, int blockSize = 0)
	{
		fixed (byte* bufferP = buffer)
			return decoder.Decode(bufferP + offset, length, blockSize);
	}

	
}