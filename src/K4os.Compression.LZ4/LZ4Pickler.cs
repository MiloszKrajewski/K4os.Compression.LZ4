using System;
#if NETSTANDARD2_1
using System.Buffers;
#endif
using System.IO;
using K4os.Compression.LZ4.Internal;

namespace K4os.Compression.LZ4
{
	/// <summary>
	/// Pickling support with LZ4 compression.
	/// </summary>
	public static class LZ4Pickler
	{

#if NETSTANDARD2_1
		/// <summary>Compresses input buffer into self-contained package.</summary>
		/// <param name="source">Input buffer.</param>
		/// <param name="level">Compression level.</param>
		/// <returns>Output buffer.</returns>
		public static byte[] Pickle(byte[] source, LZ4Level level = LZ4Level.L00_FAST) =>
			Pickle(source.AsSpan(), level);

		/// <summary>Compresses input buffer into self-contained package.</summary>
		/// <param name="source">Input buffer.</param>
		/// <param name="index">Input buffer offset.</param>
		/// <param name="count">Input buffer length.</param>
		/// <param name="level">Compression level.</param>
		/// <returns>Output buffer.</returns>
		public static byte[] Pickle(byte[] source, int index, int count, LZ4Level level = LZ4Level.L00_FAST) =>
			Pickle(source.AsSpan(index, count), level);

		/// <summary>Compresses input buffer into self-contained package.</summary>
		/// <param name="source">Input buffer.</param>
		/// <param name="count">Length of input data.</param>
		/// <param name="level">Compression level.</param>
		/// <returns>Output buffer.</returns>
		public static unsafe byte[] Pickle(byte* source, int count, LZ4Level level = LZ4Level.L00_FAST) =>
			Pickle(new Span<byte>(source, count), level);

		/// <summary>Compresses input buffer into self-contained package.</summary>
		/// <param name="source">Input buffer.</param>
		/// <param name="level">Compression level.</param>
		/// <returns>Output buffer.</returns>
		public static unsafe byte[] Pickle(ReadOnlySpan<byte> source, LZ4Level level = LZ4Level.L00_FAST)
		{
			var sourceLength = source.Length;
			if (sourceLength == 0)
			{
				return Array.Empty<byte>();
			}

			var tmp = Mem.Alloc(sourceLength);
			try
			{
				int encodedLength = LZ4Codec.Encode(source, new Span<byte>(tmp, sourceLength), level);
				if (encodedLength <= 0)
				{
					var result = new byte[sourceLength + 1];
					EmitUncompressedPreamble(result);
					source.CopyTo(result.AsSpan(1));
					return result;
				}
				else
				{
					int diffBytes = CalcDiffBytes(sourceLength - encodedLength);
					var result = new byte[1 + diffBytes + encodedLength];
					var dst = result.AsSpan();
					var src = new Span<byte>(tmp, encodedLength);
					EmitCompressedPreamble(dst, sourceLength - encodedLength, diffBytes);
					src.CopyTo(dst[(1 + diffBytes)..]);

					return result;
				}
			}
			finally
			{
				Mem.Free(tmp);
			}
		}

		/// <summary>Compresses input buffer into self-contained package.</summary>
		/// <param name="source">Input buffer.</param>
		/// <param name="writer">Where the compressed data is written.</param>
		/// <param name="level">Compression level.</param>
		/// <returns>Output buffer.</returns>
		public static void Pickle(ReadOnlySpan<byte> source, IBufferWriter<byte> writer, LZ4Level level = LZ4Level.L00_FAST)
		{
			var sourceLength = source.Length;
			if (sourceLength == 0)
			{
				return;
			}

			int diffBytes = CalcDiffBytes(sourceLength);
			var dst = writer.GetSpan(1 + diffBytes + sourceLength);

			int encodedLength = LZ4Codec.Encode(source, dst.Slice(1 + diffBytes, sourceLength), level);
			if (encodedLength <= 0)
			{
				EmitUncompressedPreamble(dst);
				source.CopyTo(dst[1..]);
				writer.Advance(1 + sourceLength);
			}
			else
			{
				EmitCompressedPreamble(dst, sourceLength - encodedLength, diffBytes);
				writer.Advance(1 + diffBytes + encodedLength);
			}
		}

		private static int CalcDiffBytes(int sourceLength)
		{
			if (sourceLength > 0xffff)
			{
				return 4;
			}
			else if (sourceLength > 0xff)
			{
				return 2;
			}

			return 1;
		}

		/// <summary>Decompresses previously pickled buffer (see: <see cref="LZ4Pickler"/>.</summary>
		/// <param name="source">Input buffer.</param>
		/// <returns>Output buffer.</returns>
		public static byte[] Unpickle(byte[] source) =>
			Unpickle(source.AsSpan());

		/// <summary>Decompresses previously pickled buffer (see: <see cref="LZ4Pickler"/>.</summary>
		/// <param name="source">Input buffer.</param>
		/// <param name="index">Input buffer offset.</param>
		/// <param name="count">Input buffer length.</param>
		/// <returns>Output buffer.</returns>
		public static unsafe byte[] Unpickle(byte[] source, int index, int count) =>
			Unpickle(source.AsSpan(index, count));

		/// <summary>Decompresses previously pickled buffer (see: <see cref="LZ4Pickler"/>.</summary>
		/// <param name="source">Input buffer.</param>
		/// <param name="count">Input buffer length.</param>
		/// <returns>Output buffer.</returns>
		public static unsafe byte[] Unpickle(byte* source, int count) =>
			Unpickle(new Span<byte>(source, count));

		/// <summary>Decompresses previously pickled buffer (see: <see cref="LZ4Pickler"/>.</summary>
		/// <param name="source">Input buffer.</param>
		/// <returns>Output buffer.</returns>
		public static byte[] Unpickle(ReadOnlySpan<byte> source)
		{
			var size = UnpickledSize(source);
			if (size == 0)
			{
				return Array.Empty<byte>();
			}

			var output = new byte[size];
			UnpickleCore(source, output, size);
			return output;
		}

		/// <summary>Decompresses previously pickled buffer (see: <see cref="LZ4Pickler"/>.</summary>
		/// <param name="source">Input buffer.</param>
		/// <param name="writer">Where the decompressed data is written.</param>
		public static void Unpickle(ReadOnlySpan<byte> source, IBufferWriter<byte> writer)
		{
			if (source.Length == 0)
			{
				return;
			}

			var size = UnpickledSize(source);
			var output = writer.GetSpan(size);
			UnpickleCore(source, output, size);
			writer.Advance(size);
		}

		/// <summary>
		/// Returns the uncompressed size of a chunk of compressed data.
		/// </summary>
		/// <param name="source">The data to inspect.</param>
		/// <returns>The size in bytes of the data once unpickled.</returns>
		public static int UnpickledSize(ReadOnlySpan<byte> source)
		{
			var sourceLength = source.Length;
			if (sourceLength == 0)
			{
				return 0;
			}

			var (version, diffBytes) = DecodeHeader(source[0]);
			if (version != 0)
			{
				throw new InvalidDataException($"Pickle format {version} is not supported");
			}

			if (sourceLength <= diffBytes)
			{
				throw CorruptedPickle("Source buffer is too small.");
			}

			return sourceLength - 1 - diffBytes + ExtractDiff(source, diffBytes);
		}

		/// <summary>Decompresses previously pickled buffer (see: <see cref="LZ4Pickler"/>.</summary>
		/// <param name="source">Input buffer.</param>
		/// <param name="output">Where the decompressed data is written.</param>
		/// <remarks>
		/// You obtain the size of the output buffer by calling <see cref="UnpickledSize(ReadOnlySpan{byte})"/>.
		/// </remarks>
		public static void Unpickle(ReadOnlySpan<byte> source, Span<byte> output)
		{
			var sourceLength = source.Length;
			if (sourceLength == 0)
			{
				return;
			}

			var (version, diffBytes) = DecodeHeader(source[0]);
			if (version != 0)
			{
				throw new InvalidDataException($"Pickling format {version} is not supported");
			}

			if (sourceLength <= diffBytes)
			{
				throw CorruptedPickle("Source buffer is too small.");
			}

			UnpickleCore(source, output, sourceLength - 1 - diffBytes + ExtractDiff(source, diffBytes));
		}

		private static void UnpickleCore(ReadOnlySpan<byte> source, Span<byte> output, int expectedLength)
		{
			var (_, diffBytes) = DecodeHeader(source[0]);
			if (source.Length == expectedLength + 1)
			{
				source[(1 + diffBytes)..].CopyTo(output);
				return;
			}

			var src = source[(1 + diffBytes)..];
			var decodedLength = LZ4Codec.Decode(src, output);
			if (decodedLength != expectedLength)
			{
				throw CorruptedPickle($"Expected to decode {expectedLength} bytes but {decodedLength} has been decoded");
			}
		}

		private static int ExtractDiff(ReadOnlySpan<byte> source, int diffBytes)
		{
			return diffBytes switch
			{
				1 => source[1],
				2 => source[1] + (source[2] << 8),
				4 => source[1] + (source[2] << 8) + (source[3] << 16) + (source[4] << 24),
				_ => 0
			};
		}

		private static void EmitUncompressedPreamble(Span<byte> result)
		{
			result[0] = EncodeHeader(0, 0);
		}

		private static void EmitCompressedPreamble(Span<byte> result, int length, int diffBytes)
		{
			switch (diffBytes)
			{
				case 1:
					result[0] = EncodeHeader(0, 1);
					result[1] = (byte)length;
					break;

				case 2:
					result[0] = EncodeHeader(0, 2);
					result[1] = (byte)(length & 0xff);
					result[2] = (byte)(length >> 8);
					break;

				case 4:
					result[0] = EncodeHeader(0, 4);
					result[1] = (byte)(length & 0xff);
					result[2] = (byte)((length >> 8) & 0xff);
					result[3] = (byte)((length >> 16) & 0xff);
					result[4] = (byte)(length >> 24);
					break;
			}
		}

		private static byte EncodeHeader(int version, byte diffBytes)
		{
			if (diffBytes == 4)
			{
				diffBytes = 3;
			}

			return (byte)((version & 0x07) | ((diffBytes & 0x3) << 6));
		}

		private static (int version, byte lengthBtes) DecodeHeader(byte header)
		{
			var len = (header >> 6) & 0x3;
			if (len == 3)
			{
				len++;
			}

			return (header & 0x7, (byte)len);
		}

#else  // version for legacy frameworks

		private const byte VersionMask = 0x07;
		private const byte CurrentVersion = 0 & VersionMask; // 3 bits

		/// <summary>Compresses input buffer into self-contained package.</summary>
		/// <param name="source">Input buffer.</param>
		/// <param name="level">Compression level.</param>
		/// <returns>Output buffer.</returns>
		public static byte[] Pickle(byte[] source, LZ4Level level = LZ4Level.L00_FAST) =>
			Pickle(source, 0, source.Length, level);

		/// <summary>Compresses input buffer into self-contained package.</summary>
		/// <param name="source">Input buffer.</param>
		/// <param name="sourceOffset">Input buffer offset.</param>
		/// <param name="sourceLength">Input buffer length.</param>
		/// <param name="level">Compression level.</param>
		/// <returns>Output buffer.</returns>
		public static unsafe byte[] Pickle(
			byte[] source, int sourceOffset, int sourceLength,
			LZ4Level level = LZ4Level.L00_FAST)
		{
			source.Validate(sourceOffset, sourceLength);
			fixed (byte* sourceP = source)
				return Pickle(sourceP + sourceOffset, sourceLength, level);
		}

		/// <summary>Compresses input buffer into self-contained package.</summary>
		/// <param name="source">Input buffer.</param>
		/// <param name="level">Compression level.</param>
		/// <returns>Output buffer.</returns>
		public static unsafe byte[] Pickle(
			ReadOnlySpan<byte> source, LZ4Level level = LZ4Level.L00_FAST)
		{
			var sourceLength = source.Length;
			if (sourceLength <= 0)
				return Mem.Empty;

			fixed (byte* sourceP = &MemoryMarshal.GetReference(source))
				return Pickle(sourceP, sourceLength, level);
		}

		/// <summary>Compresses input buffer into self-contained package.</summary>
		/// <param name="source">Input buffer.</param>
		/// <param name="sourceLength">Length of input data.</param>
		/// <param name="level">Compression level.</param>
		/// <returns>Output buffer.</returns>
		public static unsafe byte[] Pickle(
			byte* source, int sourceLength, LZ4Level level = LZ4Level.L00_FAST)
		{
			if (sourceLength <= 0)
				return Mem.Empty;

			var targetLength = sourceLength - 1;
			var target = (byte*) Mem.Alloc(sourceLength);
			try
			{
				var encodedLength = LZ4Codec.Encode(
					source, sourceLength, target, targetLength, level);

				return encodedLength <= 0
					? PickleV0(source, sourceLength, sourceLength)
					: PickleV0(target, encodedLength, sourceLength);
			}
			finally
			{
				Mem.Free(target);
			}
		}

		/// <summary>Decompresses previously pickled buffer (see: <see cref="LZ4Pickler"/>.</summary>
		/// <param name="source">Input buffer.</param>
		/// <returns>Output buffer.</returns>
		public static byte[] Unpickle(byte[] source) =>
			Unpickle(source, 0, source.Length);

		/// <summary>Decompresses previously pickled buffer (see: <see cref="LZ4Pickler"/>.</summary>
		/// <param name="source">Input buffer.</param>
		/// <param name="sourceOffset">Input buffer offset.</param>
		/// <param name="sourceLength">Input buffer length.</param>
		/// <returns>Output buffer.</returns>
		public static unsafe byte[] Unpickle(
			byte[] source, int sourceOffset, int sourceLength)
		{
			source.Validate(sourceOffset, sourceLength);

			if (sourceLength <= 0)
				return Mem.Empty;

			fixed (byte* sourceP = source)
				return Unpickle(sourceP + sourceOffset, sourceLength);
		}

		/// <summary>Decompresses previously pickled buffer (see: <see cref="LZ4Pickler"/>.</summary>
		/// <param name="source">Input buffer.</param>
		/// <returns>Output buffer.</returns>
		public static unsafe byte[] Unpickle(ReadOnlySpan<byte> source)
		{
			var sourceLength = source.Length;
			if (sourceLength <= 0)
				return Mem.Empty;

			fixed (byte* sourceP = &MemoryMarshal.GetReference(source))
				return Unpickle(sourceP, source.Length);
		}

		/// <summary>Decompresses previously pickled buffer (see: <see cref="LZ4Pickler"/>.</summary>
		/// <param name="source">Input buffer.</param>
		/// <param name="sourceLength">Input buffer length.</param>
		/// <returns>Output buffer.</returns>
		public static unsafe byte[] Unpickle(byte* source, int sourceLength)
		{
			if (sourceLength <= 0)
				return Mem.Empty;

			var flags = *source;
			var version = flags & VersionMask; // 3 bits

			if (version == 0)
				return UnpickleV0(flags, source + 1, sourceLength - 1);

			throw new InvalidDataException($"Pickle version {version} is not supported");
		}

		[SuppressMessage("ReSharper", "IdentifierTypo")]
		private static unsafe byte[] PickleV0(
			byte* target, int targetLength, int sourceLength)
		{
			var diff = sourceLength - targetLength;
			var llen = diff == 0 ? 0 : diff < 0x100 ? 1 : diff < 0x10000 ? 2 : 4;
			var result = new byte[targetLength + 1 + llen];

			fixed (byte* resultP = result)
			{
				var llenFlags = llen == 4 ? 3 : llen; // 2 bits
				var flags = (byte) ((llenFlags << 6) | CurrentVersion);
				Mem.Poke1(resultP + 0, flags);
				if (llen == 1) Mem.Poke1(resultP + 1, (byte) diff);
				else if (llen == 2) Mem.Poke2(resultP + 1, (ushort) diff);
				else if (llen == 4) Mem.Poke4(resultP + 1, (uint) diff);
				Mem.Move(resultP + llen + 1, target, targetLength);
			}

			return result;
		}

		private static unsafe byte[] UnpickleV0(
			byte flags, byte* source, int sourceLength)
		{
			// ReSharper disable once IdentifierTypo
			var llen = (flags >> 6) & 0x03; // 2 bits
			if (llen == 3) llen = 4;

			if (sourceLength < llen)
				throw CorruptedPickle("Source buffer is too small.");

			var diff = (int) (
				llen == 0 ? 0 :
				llen == 1 ? Mem.Peek1(source) :
				llen == 2 ? Mem.Peek2(source) :
				llen == 4 ? Mem.Peek4(source) :
				throw CorruptedPickle("Unexpected length descriptor.")
			);
			source += llen;
			sourceLength -= llen;
			var targetLength = sourceLength + diff;

			var target = new byte[targetLength];
			fixed (byte* targetP = target)
			{
				if (diff == 0)
				{
					Mem.Copy(targetP, source, targetLength);
				}
				else
				{
					var decodedLength = LZ4Codec.Decode(
						source, sourceLength, targetP, targetLength);
					if (decodedLength != targetLength)
						throw new ArgumentException(
							$"Expected {targetLength} bytes but {decodedLength} has been decoded");
				}
			}

			return target;
		}
#endif

		private static Exception CorruptedPickle(string message) =>
			new InvalidDataException($"Pickle is corrupted: {message}");
	}
}
