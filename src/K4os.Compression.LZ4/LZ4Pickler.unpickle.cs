using System;
using System.Buffers;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using K4os.Compression.LZ4.Internal;

namespace K4os.Compression.LZ4
{
	/// <summary>
	/// Pickling support with LZ4 compression.
	/// </summary>
	public static partial class LZ4Pickler
	{
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
			if (source.Length == 0) return Mem.Empty;
			
			var header = DecodeHeader(source);
			var size = UnpickledSize(header);
			if (size == 0) return Mem.Empty;

			var output = new byte[size];
			UnpickleCore(header, source, output);
			return output;
		}

		/// <summary>Decompresses previously pickled buffer (see: <see cref="LZ4Pickler"/>.</summary>
		/// <param name="source">Input buffer.</param>
		/// <param name="writer">Where the decompressed data is written.</param>
		public static void Unpickle(ReadOnlySpan<byte> source, IBufferWriter<byte> writer)
		{
			if (writer is null) 
				throw new ArgumentNullException(nameof(writer));

			var sourceLength = source.Length;
			if (sourceLength == 0) return;
			
			var header = DecodeHeader(source);
			var size = UnpickledSize(header);
			var output = writer.GetSpan(size).Slice(0, size);
			UnpickleCore(header, source, output);
			writer.Advance(size);
		}

		/// <summary>
		/// Returns the uncompressed size of a chunk of compressed data.
		/// </summary>
		/// <param name="source">The data to inspect.</param>
		/// <returns>The size in bytes of the data once unpickled.</returns>
		public static int UnpickledSize(ReadOnlySpan<byte> source) =>
			UnpickledSize(DecodeHeader(source));

		/// <summary>
		/// Returns the uncompressed size of a chunk of compressed data.
		/// </summary>
		/// <param name="header">Decoded header.</param>
		/// <returns>The size in bytes of the data once unpickled.</returns>
		private static int UnpickledSize(in PickleHeader header) =>
			header.ResultLength;

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

			var header = DecodeHeader(source);
			UnpickleCore(header, source, output);
		}

		private static void UnpickleCore(
			in PickleHeader header, ReadOnlySpan<byte> source, Span<byte> target)
		{
			var data = source.Slice(header.DataOffset);
			var expectedLength = UnpickledSize(header);
			var targetLength = target.Length;
			if (targetLength != expectedLength)
				throw CorruptedPickle(
					$"Output buffer size ({targetLength}) does not match expected value ({expectedLength})");

			if (!header.IsCompressed) // not compressed
			{
				data.CopyTo(target);
				return;
			}

			var decodedLength = LZ4Codec.Decode(data, target);
			if (decodedLength != expectedLength)
				throw CorruptedPickle(
					$"Expected to decode {expectedLength} bytes but {decodedLength} has been decoded");
		}

		private static PickleHeader DecodeHeader(ReadOnlySpan<byte> source) =>
			(source[0] & VersionMask) switch {
				0 => DecodeHeaderV0(source),
				var v => throw CorruptedPickle($"Version {v} is not recognized")
			};

		private static PickleHeader DecodeHeaderV0(ReadOnlySpan<byte> source)
		{
			var header = source[0];
			var sizeOfDiff = ((header >> 6) & 0x3) switch { 3 => 4, var x => x };
			var dataOffset = (ushort) (1 + sizeOfDiff);
			var dataLength = source.Length - dataOffset;
			if (dataLength < 0)
				throw CorruptedPickle($"Unexpected data length: {dataLength}");
			var resultDiff = sizeOfDiff == 0 ? 0 : PeekN(source.Slice(1), sizeOfDiff);
			var resultLength = dataLength + resultDiff;
			return new PickleHeader(dataOffset, resultLength, resultDiff != 0);
		}

		private static unsafe int PeekN(ReadOnlySpan<byte> bytes, int size)
		{
			int result = default; // just to make sure it is int 0
			if (size < 0 || size > sizeof(int) || size > bytes.Length)
				throw CorruptedPickle($"Unexpected field size: {size}");
			fixed (byte* bytesP = bytes) 
				Unsafe.CopyBlockUnaligned(&result, bytesP, (uint) size);
			return result;
		}
		
		private static Exception CorruptedPickle(string message) =>
			new InvalidDataException($"Pickle is corrupted: {message}");
	}

	[StructLayout(LayoutKind.Sequential, Pack = 1)]
	internal readonly struct PickleHeader
	{
		public ushort DataOffset { get; }
		public ushort Flags { get; }
		public int ResultLength { get; }
		public bool IsCompressed => (Flags & 0x0001) != 0;

		public PickleHeader(ushort dataOffset, int resultLength, bool compressed)
		{
			DataOffset = dataOffset;
			ResultLength = resultLength;
			Flags = (ushort) (
				(compressed ? 0x0001 : 0x0000) << 0 |
				0
			);
		}
	}
}
