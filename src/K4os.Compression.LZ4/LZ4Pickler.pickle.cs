#nullable enable

using System;
using System.Buffers;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using K4os.Compression.LZ4.Internal;

namespace K4os.Compression.LZ4;

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
			Span<byte> target = stackalloc byte[MAX_STACKALLOC];
			return PickleWithBuffer(source, level, target);
		}
		else
		{
			PinnedMemory.Alloc(out var target, sourceLength, false);
			try
			{
				return PickleWithBuffer(source, level, target.Span);
			}
			finally
			{
				target.Free();
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
			var target = result.AsSpan();
			var offset = EncodeUncompressedHeader(target, version, sourceLength);
			Debug.Assert(headerSize == offset, "Unexpected header size");
			source.CopyTo(target.Slice(offset));
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
	public static void Pickle<TBufferWriter>(
		ReadOnlySpan<byte> source, TBufferWriter writer,
		LZ4Level level = LZ4Level.L00_FAST)
		where TBufferWriter: IBufferWriter<byte>
	{
		if (writer is null) 
			throw new ArgumentNullException(nameof(writer));

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

	/// <summary>Compresses input buffer into self-contained package.</summary>
	/// <param name="source">Input buffer.</param>
	/// <param name="writer">Where the compressed data is written.</param>
	/// <param name="level">Compression level.</param>
	/// <returns>Output buffer.</returns>
	public static void Pickle(
		ReadOnlySpan<byte> source, IBufferWriter<byte> writer,
		LZ4Level level = LZ4Level.L00_FAST) =>
		Pickle<IBufferWriter<byte>>(source, writer, level);

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

	private static unsafe void PokeN(Span<byte> target, int value, int size)
	{
		if (size < 0 || size > sizeof(int) || target.Length < size)
			throw new ArgumentException($"Unexpected size: {size}");
		Unsafe.CopyBlockUnaligned(ref target[0], ref *(byte*)&value, (uint) size);
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