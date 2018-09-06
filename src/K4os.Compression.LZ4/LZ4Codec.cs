using System;
using System.IO;
using K4os.Compression.LZ4.Internal;

namespace K4os.Compression.LZ4
{
	public class LZ4Codec
	{
		/// <summary>
		/// Maximum size after compression.
		/// </summary>
		/// <param name="length">Length of input data.</param>
		/// <returns>Maximum length after compression.</returns>
		public static int MaximumOutputSize(int length) => LZ4_xx.LZ4_compressBound(length);

		/// <summary>
		/// Compress data of given length.
		/// </summary>
		/// <param name="source">Input buffer, data to be compressed.</param>
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

			var encoded =
				level == LZ4Level.L00_FAST
					? LZ4_64.LZ4_compress_default(source, target, sourceLength, targetLength)
					: LZ4_64_HC.LZ4_compress_HC(
						source, target, sourceLength, targetLength, (int) level);
			return encoded <= 0 ? -1 : encoded;
		}

		/// <summary>
		/// Compress data of given length.
		/// </summary>
		/// <param name="source">Input buffer, data to be compressed.</param>
		/// <param name="sourceIndex">Starting index of input buffer.</param>
		/// <param name="sourceLength">Length of input buffer.</param>
		/// <param name="target">Output buffer.</param>
		/// <param name="targetIndex">Starting index of output buffer.</param>
		/// <param name="targetLength">Output buffer length.</param>
		/// <param name="level">Compression level.</param>
		/// <returns>Number of bytes written, or negative value if output buffer is too small.</returns>
		public static unsafe int Encode(
			byte[] source, int sourceIndex, int sourceLength,
			byte[] target, int targetIndex, int targetLength,
			LZ4Level level = LZ4Level.L00_FAST)
		{
			Validate(source, sourceIndex, sourceLength);
			Validate(target, targetIndex, targetLength);
			fixed (byte* sourceP = &source[sourceIndex])
			fixed (byte* targetP = &target[targetIndex])
				return Encode(sourceP, sourceLength, targetP, targetLength, level);
		}

		/// <summary>
		/// Decompress data from given buffer.
		/// </summary>
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

			var decoded = LZ4_xx.LZ4_decompress_safe(source, target, sourceLength, targetLength);
			return decoded <= 0 ? -1 : decoded;
		}

		/// <summary>
		/// Decompress data from given buffer.
		/// </summary>
		/// <param name="source">Input buffer.</param>
		/// <param name="sourceIndex">Starting index in input buffer.</param>
		/// <param name="sourceLength">Input buffer length.</param>
		/// <param name="target">Output buffer.</param>
		/// <param name="targetIndex">Starting index in output buffer.</param>
		/// <param name="targetLength">Output buffer length.</param>
		/// <returns>Number of bytes written, or negative value if output buffer is too small.</returns>
		public static unsafe int Decode(
			byte[] source, int sourceIndex, int sourceLength,
			byte[] target, int targetIndex, int targetLength)
		{
			Validate(source, sourceIndex, sourceLength);
			Validate(target, targetIndex, targetLength);
			fixed (byte* sourceP = &source[sourceIndex])
			fixed (byte* targetP = &target[targetIndex])
				return Decode(sourceP, sourceLength, targetP, targetLength);
		}
		
		/// <summary>
		/// Creates a byte array with compressed data. This array contains everything is needed to
		/// decompress it. For example, you can encode it with base64 and send it via email.
		/// </summary>
		/// <param name="source">Input buffer.</param>
		/// <param name="level">Compression level.</param>
		/// <returns>Compressed input buffer.</returns>
		public static byte[] Pickle(byte[] source, LZ4Level level = LZ4Level.L00_FAST) =>
			Pickle(source, 0, source.Length);

		/// <summary>
		/// Creates a byte array with compressed data. This array contains everything is needed to
		/// decompress it. For example, you can encode it with base64 and send it via email.
		/// </summary>
		/// <param name="source">Input buffer.</param>
		/// <param name="sourceIndex">Starting index in input buffer.</param>
		/// <param name="sourceLength">Length of input data.</param>
		/// <param name="level">Compression level.</param>
		/// <returns>Compressed input buffer.</returns>
		public static unsafe byte[] Pickle(
			byte[] source, int sourceIndex, int sourceLength,
			LZ4Level level = LZ4Level.L00_FAST)
		{
			if (sourceLength <= 0)
				return Array.Empty<byte>();

			Validate(source, sourceIndex, sourceLength);

			var targetLength = sourceLength - 1;
			var targetP = (byte*) Mem.Alloc(sourceLength);
			try
			{
				fixed (byte* sourceP = source)
				{
					var encodedLength = Encode(sourceP, sourceLength, targetP, targetLength, level);

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

		private const byte VersionMask = 0x07;
		private const byte CurrentVersion = 0 & VersionMask; // 3 bits

		private static unsafe byte[] PickleCopy(
			byte* target, int targetLength, int sourceLength)
		{
			var diff = sourceLength - targetLength;
			// ReSharper disable once IdentifierTypo
			var llen = diff == 0 ? 0 : diff < 0x100 ? 1 : diff < 0x10000 ? 2 : 4;
			var result = new byte[targetLength + 1 + llen];

			fixed (byte* resultP = result)
			{
				var flags = (byte) (((llen == 4 ? 3 : llen) << 6) | CurrentVersion);
				Mem.Poke8(resultP + 0, flags);
				if (llen == 1) Mem.Poke8(resultP + 1, (byte) diff);
				else if (llen == 2) Mem.Poke16(resultP + 1, (ushort) diff);
				else if (llen == 4) Mem.Poke32(resultP + 1, (uint) diff);
				Mem.Move(resultP + llen + 1, target, targetLength);
			}

			return result;
		}

		/// <summary>
		/// Decompresses previously pickled buffer (using <c>Pickle</c>).
		/// </summary>
		/// <param name="source">Input buffer.</param>
		/// <returns>Decompressed input buffer.</returns>
		public static byte[] Unpickle(byte[] source) =>
			Unpickle(source, 0, source.Length);

		/// <summary>
		/// Decompresses previously pickled buffer (using <c>Pickle</c>).
		/// </summary>
		/// <param name="source">Input buffer.</param>
		/// <param name="sourceIndex">Starting index in input buffer.</param>
		/// <param name="sourceLength">Input data length.</param>
		/// <returns>Decompressed input buffer.</returns>
		public static byte[] Unpickle(
			byte[] source, int sourceIndex, int sourceLength)
		{
			if (sourceLength <= 0)
				return Array.Empty<byte>();

			var flags = source[sourceIndex];
			var version = flags & VersionMask; // 3 bits
			if (version == 0)
				return UnpickleV0(flags, source, sourceIndex + 1, sourceLength - 1);

			throw new InvalidDataException($"Pickle version {version} is not supported");
		}

		private static byte[] UnpickleV0(
			byte flags, byte[] source, int sourceIndex, int sourceLength)
		{
			// ReSharper disable once IdentifierTypo
			var llen = (flags >> 6) & 0x03; // 2 bits
			if (llen == 3) llen = 4;
			var diff = (int) (
				llen == 0 ? 0 :
				llen == 1 ? source[sourceIndex] :
				llen == 2 ? BitConverter.ToUInt16(source, sourceIndex) :
				llen == 4 ? BitConverter.ToUInt32(source, sourceIndex) :
				throw new InvalidDataException("Pickled data is corrupted.")
			);
			sourceIndex += llen;
			sourceLength -= llen;
			var targetLength = sourceLength + diff;

			var target = new byte[targetLength];

			if (diff == 0)
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
	}
}
