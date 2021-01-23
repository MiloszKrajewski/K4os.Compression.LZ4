using System;
using System.Buffers;
using System.Diagnostics;
using System.IO;
using K4os.Compression.LZ4.Internal;

namespace K4os.Compression.LZ4
{
	/// <summary>
	/// Pickling support with LZ4 compression.
	/// </summary>
	public static class LZ4PicklerV2
	{
		private const int MAX_STACKALLOC = 1024;

		/// <summary>Compresses input buffer into self-contained package.</summary>
		/// <param name="source">Input buffer.</param>
		/// <param name="level">Compression level.</param>
		/// <returns>Output buffer.</returns>
		public static byte[] Pickle(byte[] source, LZ4Level level = LZ4Level.L00_FAST) =>
			Pickle(source.AsSpan(), level);

		/// <summary>Compresses input buffer into self-contained package.</summary>
		/// <param name="source">Input buffer.</param>
		/// <param name="sourceIndex">Input buffer offset.</param>
		/// <param name="sourceLength">Input buffer length.</param>
		/// <param name="level">Compression level.</param>
		/// <returns>Output buffer.</returns>
		public static byte[] Pickle(
			byte[] source, int sourceIndex, int sourceLength,
			LZ4Level level = LZ4Level.L00_FAST) =>
			Pickle(source.AsSpan(sourceIndex, sourceLength), level);

		/// <summary>Compresses input buffer into self-contained package.</summary>
		/// <param name="source">Input buffer.</param>
		/// <param name="length">Length of input data.</param>
		/// <param name="level">Compression level.</param>
		/// <returns>Output buffer.</returns>
		public static unsafe byte[] Pickle(
			byte* source, int length, LZ4Level level = LZ4Level.L00_FAST) =>
			Pickle(new Span<byte>(source, length), level);

		/// <summary>Compresses input buffer into self-contained package.</summary>
		/// <param name="source">Input buffer.</param>
		/// <param name="level">Compression level.</param>
		/// <returns>Output buffer.</returns>
		public static unsafe byte[] Pickle(
			ReadOnlySpan<byte> source, LZ4Level level = LZ4Level.L00_FAST)
		{
			var sourceLength = source.Length;
			if (sourceLength == 0) return Mem.Empty;

			if (sourceLength < MAX_STACKALLOC)
			{
				var buffer = stackalloc byte[MAX_STACKALLOC];
				return PickleWithBuffer(source, level, new Span<byte>(buffer, sourceLength));
			}
			else
			{
				var buffer = Mem.Alloc(sourceLength);
				try
				{
					return PickleWithBuffer(source, level, new Span<byte>(buffer, sourceLength));
				}
				finally
				{
					Mem.Free(buffer);
				}
			}
		}

		private static byte[] PickleWithBuffer(
			ReadOnlySpan<byte> source, LZ4Level level, Span<byte> buffer)
		{
			var sourceLength = source.Length;

			Debug.Assert(buffer.Length >= sourceLength);
			var encodedLength = LZ4Codec.Encode(source, buffer, level);

			if (encodedLength <= 0 || encodedLength > sourceLength)
			{
				var result = new byte[sourceLength + 1];
				var offset = EmitUncompressedPreamble(result);
				source.CopyTo(result.AsSpan(offset));
				return result;
			}
			else
			{
				var diffBytes = CalcDiffBytes(sourceLength - encodedLength);
				var result = new byte[1 + diffBytes + encodedLength];
				var target = result.AsSpan();
				var offset = EmitCompressedPreamble(
					target, sourceLength - encodedLength, diffBytes);
				buffer.Slice(0, encodedLength).CopyTo(target.Slice(offset));
				return result;
			}
		}

		/// <summary>Compresses input buffer into self-contained package.</summary>
		/// <param name="source">Input buffer.</param>
		/// <param name="writer">Where the compressed data is written.</param>
		/// <param name="level">Compression level.</param>
		/// <returns>Output buffer.</returns>
		public static void Pickle(
			ReadOnlySpan<byte> source, IBufferWriter<byte> writer,
			LZ4Level level = LZ4Level.L00_FAST)
		{
			var sourceLength = source.Length;
			if (sourceLength == 0) return;

			// number of bytes is not decided on diff but rather of full length
			// although, diff would never be greater than full length
			var diffBytes = CalcDiffBytes(sourceLength);
			var target = writer.GetSpan(1 + diffBytes + sourceLength);

			var encodedLength = LZ4Codec.Encode(
				source, target.Slice(1 + diffBytes, sourceLength), level);

			if (encodedLength <= 0)
			{
				var offset = EmitUncompressedPreamble(target);
				source.CopyTo(target.Slice(offset));
				writer.Advance(offset + sourceLength);
			}
			else
			{
				var offset = EmitCompressedPreamble(
					target, sourceLength - encodedLength, diffBytes);
				writer.Advance(offset + encodedLength);
			}
		}

		private static int CalcDiffBytes(int sourceLength) =>
			sourceLength switch { > 0xffff => 4, > 0xff => 2, _ => 1 };

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
		public static byte[] Unpickle(byte[] source, int index, int count) =>
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
			if (size == 0) return Mem.Empty;

			var output = new byte[size];
			UnpickleCore(source, output, size);
			return output;
		}

		/// <summary>Decompresses previously pickled buffer (see: <see cref="LZ4Pickler"/>.</summary>
		/// <param name="source">Input buffer.</param>
		/// <param name="writer">Where the decompressed data is written.</param>
		public static void Unpickle(ReadOnlySpan<byte> source, IBufferWriter<byte> writer)
		{
			if (source.Length == 0) return;

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
			if (sourceLength == 0) return 0;

			var header = DecodeHeader(source[0]);
			var version = header.Version;
			var diffBytes = header.SizeOfDiff;
			if (version != 0)
				throw new InvalidDataException($"Pickle format {version} is not supported");

			if (sourceLength <= diffBytes)
				throw CorruptedPickle("Source buffer is too small.");

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
			if (sourceLength == 0) return;

			var header = DecodeHeader(source[0]);
			var version = header.Version;
			var diffBytes = header.SizeOfDiff;
			if (version != 0)
				throw new InvalidDataException($"Pickling format {version} is not supported");

			if (sourceLength <= diffBytes)
				throw CorruptedPickle("Source buffer is too small.");

			UnpickleCore(
				source, output, sourceLength - 1 - diffBytes + ExtractDiff(source, diffBytes));
		}

		private static void UnpickleCore(
			ReadOnlySpan<byte> source, Span<byte> target, int expectedLength)
		{
			var header = DecodeHeader(source[0]);
			var data = source.Slice(1 + header.SizeOfDiff);
			if (source.Length == expectedLength)
			{
				data.CopyTo(target);
				return;
			}

			var decodedLength = LZ4Codec.Decode(data, target);
			if (decodedLength != expectedLength)
				throw CorruptedPickle(
					$"Expected to decode {expectedLength} bytes but {decodedLength} has been decoded");
		}

		private static int ExtractDiff(ReadOnlySpan<byte> source, int diffBytes)
		{
			return diffBytes switch {
				1 => source[1],
				2 => source[1] + (source[2] << 8),
				4 => source[1] + (source[2] << 8) + (source[3] << 16) + (source[4] << 24),
				_ => 0
			};
		}

		private static int EmitUncompressedPreamble(Span<byte> result)
		{
			result[0] = EncodeHeader(0, 0);
			return 1;
		}

		private static int EmitCompressedPreamble(Span<byte> result, int length, int diffBytes)
		{
			switch (diffBytes)
			{
				case 1:
					result[0] = EncodeHeader(0, 1);
					result[1] = (byte) length;
					return 2;
				case 2:
					result[0] = EncodeHeader(0, 2);
					result[1] = (byte) (length & 0xff);
					result[2] = (byte) (length >> 8);
					return 3;
				case 4:
					result[0] = EncodeHeader(0, 4);
					result[1] = (byte) (length & 0xff);
					result[2] = (byte) ((length >> 8) & 0xff);
					result[3] = (byte) ((length >> 16) & 0xff);
					result[4] = (byte) (length >> 24);
					return 5;
				default:
					throw new ArgumentException($"Invalid diffBytes: {diffBytes}");
			}
		}

		private static byte EncodeHeader(int version, byte diffBytes)
		{
			if (diffBytes == 4) diffBytes = 3;

			return (byte) ((version & 0x07) | ((diffBytes & 0x3) << 6));
		}

		private readonly struct Header
		{
			public int Version { get; }
			public int SizeOfDiff { get; }

			public Header(int version, int length)
			{
				Version = version;
				SizeOfDiff = length;
			}
		}

		private static Header DecodeHeader(byte header)
		{
			var len = (header >> 6) & 0x3;
			return new Header(header & 0x7, len == 3 ? 4 : len);
		}

		private static Exception CorruptedPickle(string message) =>
			new InvalidDataException($"Pickle is corrupted: {message}");
	}
}
