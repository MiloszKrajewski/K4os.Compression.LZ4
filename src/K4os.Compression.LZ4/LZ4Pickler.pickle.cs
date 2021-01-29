using System;
using System.Buffers;
using System.Diagnostics;
using K4os.Compression.LZ4.Internal;

namespace K4os.Compression.LZ4
{
	/// <summary>
	/// Pickling support with LZ4 compression.
	/// </summary>
	public static partial class LZ4Pickler
	{
		private const int MAX_STACKALLOC = 1024;

		private const byte VersionMask = 0x07;

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

			if (sourceLength <= MAX_STACKALLOC)
			{
				var buffer = stackalloc byte[MAX_STACKALLOC];
				return PickleWithBuffer(source, level, new Span<byte>(buffer, MAX_STACKALLOC));
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
			const int version = 0;
			var sourceLength = source.Length;

			Debug.Assert(buffer.Length >= sourceLength);
			var encodedLength = LZ4Codec.Encode(source, buffer, level);

			if (encodedLength <= 0 || encodedLength >= sourceLength)
			{
				var headerSize = GetUncompressedHeaderSize(version, sourceLength);
				var result = new byte[headerSize + sourceLength];
				var offset = EncodeUncompressedHeader(result.AsSpan(), version, sourceLength);
				Debug.Assert(headerSize == offset, "Unexpected header size");
				source.CopyTo(result.AsSpan(offset));
				return result;
			}
			else
			{
				var headerSize = GetCompressedHeaderSize(version, sourceLength, encodedLength);
				var result = new byte[headerSize + encodedLength];
				var target = result.AsSpan();
				var offset = EncodeCompressedHeader(
					target, version, headerSize, sourceLength, encodedLength);
				Debug.Assert(headerSize == offset, "Unexpected header size");
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

			// this might be an argument at some point
			const int version = 0;

			// number of bytes is not decided on diff but rather of full length
			// although, diff would never be greater than full length
			var headerSize = GetPessimisticHeaderSize(version, sourceLength);
			var target = writer.GetSpan(headerSize + sourceLength);

			var encodedLength = LZ4Codec.Encode(
				source, target.Slice(headerSize, sourceLength), level);

			if (encodedLength <= 0 || encodedLength >= sourceLength)
			{
				var offset = EncodeUncompressedHeader(target, version, sourceLength);
				source.CopyTo(target.Slice(offset));
				writer.Advance(offset + sourceLength);
			}
			else
			{
				var offset = EncodeCompressedHeader(
					target, version, headerSize, sourceLength, encodedLength);
				Debug.Assert(headerSize == offset, "Unexpected header size");
				writer.Advance(offset + encodedLength);
			}
		}

		// ReSharper disable once UnusedParameter.Local
		private static int GetPessimisticHeaderSize(int version, int sourceLength) =>
			version switch {
				0 => sizeof(byte) + EffectiveSizeOf(sourceLength),
				_ => throw UnexpectedVersion(version),
			};

		// ReSharper disable once UnusedParameter.Local
		private static int GetUncompressedHeaderSize(int version, int sourceLength) =>
			version switch {
				0 => sizeof(byte),
				_ => throw UnexpectedVersion(version),
			};

		private static int GetCompressedHeaderSize(
			int version, int sourceLength, int encodedLength) =>
			version switch {
				0 => sizeof(byte) + EffectiveSizeOf(sourceLength - encodedLength),
				_ => throw UnexpectedVersion(version),
			};

		// ReSharper disable once UnusedParameter.Local
		private static int EncodeUncompressedHeader(
			Span<byte> target, int version, int sourceLength) =>
			version switch {
				0 => EncodeUncompressedHeaderV0(target),
				_ => throw UnexpectedVersion(version),
			};

		private static int EncodeUncompressedHeaderV0(Span<byte> target)
		{
			// yup, that's what V0 uncompressed header looks like
			target[0] = 0; // EncodeHeaderByteV0(0);
			return 1;
		}

		private static int EncodeCompressedHeader(
			Span<byte> target, int version, int headerSize, int sourceLength, int encodedLength) =>
			version switch {
				0 => EncodeCompressedHeaderV0(target, headerSize, sourceLength, encodedLength),
				_ => throw UnexpectedVersion(version),
			};

		private static int EncodeCompressedHeaderV0(
			Span<byte> target, int headerSize, int sourceLength, int encodedLength)
		{
			var diffLength = sourceLength - encodedLength;
			var sizeOfDiff = headerSize - 1;
			Debug.Assert(EffectiveSizeOf(diffLength) <= sizeOfDiff, "Unexpected header size");
			target[0] = EncodeHeaderByteV0(sizeOfDiff);
			PokeN(target.Slice(1), diffLength, sizeOfDiff);
			return 1 + sizeOfDiff;
		}

		private static void PokeN(Span<byte> target, int value, int size)
		{
			switch (size)
			{
				case 0:
					break;
				case 1:
					target[0] = (byte) value;
					break;
				case 2:
					target[0] = (byte) value;
					target[1] = (byte) (value >> 8);
					break;
				case 4:
					target[0] = (byte) value;
					target[1] = (byte) (value >> 8);
					target[2] = (byte) (value >> 16);
					target[3] = (byte) (value >> 24);
					break;
				default:
					throw new ArgumentException($"Unexpected int size: {size}");
			}
		}

		private static byte EncodeHeaderByteV0(int sizeOfDiff) =>
			(byte) ((0 & 0x07) | ((EncodeSizeOf(sizeOfDiff) & 0x3) << 6));

		private static int EffectiveSizeOf(int value) =>
			value switch { > 0xffff or < 0 => 4, > 0xff => 2, _ => 1 };
		
		private static int EncodeSizeOf(int size) =>
			size switch { 4 => 3, _ => size };

		private static Exception UnexpectedVersion(int version) =>
			new ArgumentException($"Unexpected pickle version: {version}");
	}
}
