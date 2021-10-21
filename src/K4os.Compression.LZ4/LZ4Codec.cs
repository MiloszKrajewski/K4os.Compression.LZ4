using System;
using System.Runtime.InteropServices;
using K4os.Compression.LZ4.Engine;

namespace K4os.Compression.LZ4
{
	/// <summary>
	/// Static class exposing LZ4 block compression methods.
	/// </summary>
	public static class LZ4Codec
	{
		/// <summary>Version of LZ4 implementation.</summary>
		public const int Version = 192;

		/// <summary>
		/// Enforces 32-bit compression/decompression algorithm even on 64-bit systems.
		/// Please note, this property should not be used on regular basis, it just allows
		/// to workaround some problems on platforms which do not support 64-bit the same was
		/// as Intel (for example: unaligned read/writes).
		/// </summary>
		public static bool Enforce32
		{
			get => LL.Enforce32;
			set => LL.Enforce32 = value;
		}

		/// <summary>Maximum size after compression.</summary>
		/// <param name="length">Length of input buffer.</param>
		/// <returns>Maximum length after compression.</returns>
		public static int MaximumOutputSize(int length) =>
			LL.LZ4_compressBound(length);

		/// <summary>Compresses data from one buffer into another.</summary>
		/// <param name="source">Input buffer.</param>
		/// <param name="sourceLength">Length of input buffer.</param>
		/// <param name="target">Output buffer.</param>
		/// <param name="targetLength">Output buffer length.</param>
		/// <param name="level">Compression level.</param>
		/// <returns>Number of bytes written, or negative value if output buffer is too small.</returns>
		public static unsafe int Encode(
			byte* source, int sourceLength,
			byte* target, int targetLength,
			LZ4Level level = LZ4Level.L00_FAST)
		{
			if (sourceLength <= 0)
				return 0;

			var encoded = level < LZ4Level.L03_HC
				? LLxx.LZ4_compress_fast(source, target, sourceLength, targetLength, 1)
				: LLxx.LZ4_compress_HC(source, target, sourceLength, targetLength, (int)level);
			return encoded <= 0 ? -1 : encoded;
		}

		/// <summary>Compresses data from one buffer into another.</summary>
		/// <param name="source">Input buffer.</param>
		/// <param name="target">Output buffer.</param>
		/// <param name="level">Compression level.</param>
		/// <returns>Number of bytes written, or negative value if output buffer is too small.</returns>
		public static unsafe int Encode(
			ReadOnlySpan<byte> source, Span<byte> target,
			LZ4Level level = LZ4Level.L00_FAST)
		{
			var sourceLength = source.Length;
			if (sourceLength <= 0)
				return 0;

			var targetLength = target.Length;
			fixed (byte* sourceP = &MemoryMarshal.GetReference(source))
			fixed (byte* targetP = &MemoryMarshal.GetReference(target))
				return Encode(sourceP, sourceLength, targetP, targetLength, level);
		}

		/// <summary>Compresses data from one buffer into another.</summary>
		/// <param name="source">Input buffer.</param>
		/// <param name="sourceOffset">Input buffer offset.</param>
		/// <param name="sourceLength">Input buffer length.</param>
		/// <param name="target">Output buffer.</param>
		/// <param name="targetOffset">Output buffer offset.</param>
		/// <param name="targetLength">Output buffer length.</param>
		/// <param name="level">Compression level.</param>
		/// <returns>Number of bytes written, or negative value if output buffer is too small.</returns>
		public static unsafe int Encode(
			byte[] source, int sourceOffset, int sourceLength,
			byte[] target, int targetOffset, int targetLength,
			LZ4Level level = LZ4Level.L00_FAST)
		{
			source.Validate(sourceOffset, sourceLength);
			target.Validate(targetOffset, targetLength);
			fixed (byte* sourceP = source)
			fixed (byte* targetP = target)
				return Encode(
					sourceP + sourceOffset, sourceLength,
					targetP + targetOffset, targetLength,
					level);
		}

		/// <summary>Decompresses data from given buffer.</summary>
		/// <param name="source">Input buffer.</param>
		/// <param name="sourceLength">Input buffer length.</param>
		/// <param name="target">Output buffer.</param>
		/// <param name="targetLength">Output buffer length.</param>
		/// <returns>Number of bytes written, or negative value if output buffer is too small.</returns>
		public static unsafe int Decode(
			byte* source, int sourceLength,
			byte* target, int targetLength)
		{
			if (sourceLength <= 0)
				return 0;

			var decoded = LLxx.LZ4_decompress_safe(source, target, sourceLength, targetLength);
			return decoded <= 0 ? -1 : decoded;
		}

		/// <summary>Decompresses data from given buffer.</summary>
		/// <param name="source">Input buffer.</param>
		/// <param name="sourceLength">Input buffer length.</param>
		/// <param name="target">Output buffer.</param>
		/// <param name="targetLength">Output buffer length.</param>
		/// <param name="dictionary">Dictionary buffer.</param>
		/// <param name="dictionaryLength">Dictionary buffer length.</param>
		/// <returns>Number of bytes written, or negative value if output buffer is too small.</returns>
		public static unsafe int Decode(
			byte* source, int sourceLength,
			byte* target, int targetLength,
			byte* dictionary, int dictionaryLength)
		{
			if (sourceLength <= 0)
				return 0;

			var noDictionary = dictionary == null || dictionaryLength <= 0;
			var decoded = noDictionary
				? LLxx.LZ4_decompress_safe(
					source, target, sourceLength, targetLength)
				: LLxx.LZ4_decompress_safe_usingDict(
					source, target, sourceLength, targetLength,
					dictionary, dictionaryLength);

			return decoded <= 0 ? -1 : decoded;
		}

		/// <summary>Decompresses data from given buffer.</summary>
		/// <param name="source">Input buffer.</param>
		/// <param name="target">Output buffer.</param>
		/// <returns>Number of bytes written, or negative value if output buffer is too small.</returns>
		public static unsafe int Decode(
			ReadOnlySpan<byte> source, Span<byte> target)
		{
			var sourceLength = source.Length;
			if (sourceLength <= 0)
				return 0;

			var targetLength = target.Length;

			fixed (byte* sourceP = source)
			fixed (byte* targetP = target)
				return Decode(sourceP, sourceLength, targetP, targetLength);
		}

		/// <summary>Decompresses data from given buffer.</summary>
		/// <param name="source">Input buffer.</param>
		/// <param name="target">Output buffer.</param>
		/// <param name="dictionary">Dictionary buffer.</param>
		/// <returns>Number of bytes written, or negative value if output buffer is too small.</returns>
		public static unsafe int Decode(
			ReadOnlySpan<byte> source, Span<byte> target, ReadOnlySpan<byte> dictionary)
		{
			var sourceLength = source.Length;
			if (sourceLength <= 0)
				return 0;

			var targetLength = target.Length;
			var dictionaryLength = dictionary.Length;

			fixed (byte* sourceP = target)
			fixed (byte* targetP = target)
			fixed (byte* dictionaryP = dictionary)
				return Decode(
					sourceP, sourceLength,
					targetP, targetLength,
					dictionaryP, dictionaryLength);
		}

		/// <summary>Decompresses data from given buffer.</summary>
		/// <param name="source">Input buffer.</param>
		/// <param name="sourceOffset">Input buffer offset.</param>
		/// <param name="sourceLength">Input buffer length.</param>
		/// <param name="target">Output buffer.</param>
		/// <param name="targetOffset">Output buffer offset.</param>
		/// <param name="targetLength">Output buffer length.</param>
		/// <returns>Number of bytes written, or negative value if output buffer is too small.</returns>
		public static unsafe int Decode(
			byte[] source, int sourceOffset, int sourceLength,
			byte[] target, int targetOffset, int targetLength)
		{
			source.Validate(sourceOffset, sourceLength);
			target.Validate(targetOffset, targetLength);

			fixed (byte* sourceP = source)
			fixed (byte* targetP = target)
				return Decode(
					sourceP + sourceOffset, sourceLength,
					targetP + targetOffset, targetLength);
		}

		/// <summary>Decompresses data from given buffer.</summary>
		/// <param name="source">Input buffer.</param>
		/// <param name="sourceOffset">Input buffer offset.</param>
		/// <param name="sourceLength">Input buffer length.</param>
		/// <param name="target">Output buffer.</param>
		/// <param name="targetOffset">Output buffer offset.</param>
		/// <param name="targetLength">Output buffer length.</param>
		/// <param name="dictionary">Dictionary buffer.</param>
		/// <param name="dictionaryOffset">Dictionary buffer offset.</param>
		/// <param name="dictionaryLength">Dictionary buffer length.</param>
		/// <returns>Number of bytes written, or negative value if output buffer is too small.</returns>
		public static unsafe int Decode(
			byte[] source, int sourceOffset, int sourceLength,
			byte[] target, int targetOffset, int targetLength,
			byte[] dictionary, int dictionaryOffset, int dictionaryLength)
		{
			source.Validate(sourceOffset, sourceLength);
			target.Validate(targetOffset, targetLength);
			dictionary.Validate(dictionaryOffset, dictionaryLength);

			fixed (byte* sourceP = source)
			fixed (byte* targetP = target)
			fixed (byte* dictionaryP = dictionary)
				return Decode(
					sourceP + sourceOffset, sourceLength,
					targetP + targetOffset, targetLength,
					dictionaryP + dictionaryOffset, dictionaryLength);
		}
	}
}
