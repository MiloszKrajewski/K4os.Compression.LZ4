using System;
using System.IO;

namespace K4os.Compression.LZ4.Legacy;

/// <summary>
/// Utility class with factory methods to create legacy LZ4 (lz4net) compression and decompression streams.
/// </summary>
public static class LZ4Legacy
{
	/// <summary>Initializes a new instance of the <see cref="LZ4Stream" /> class.</summary>
	/// <param name="innerStream">The inner stream.</param>
	/// <param name="highCompression"><c>true</c> if high compression should be used,
	/// <c>false</c> otherwise.</param>
	/// <param name="blockSize">Size of the block.</param>
	/// <param name="leaveOpen">Indicates if inner stream should be left open after compression.</param>
	public static LZ4Stream Encode(
		Stream innerStream,
		bool highCompression = false,
		int blockSize = 1024 * 1024,
		bool leaveOpen = false)
	{
		var compressionFlags =
			(highCompression ? LZ4StreamFlags.HighCompression : LZ4StreamFlags.None) |
			(leaveOpen ? LZ4StreamFlags.IsolateInnerStream : LZ4StreamFlags.None) |
			LZ4StreamFlags.InteractiveRead;
		return new LZ4Stream(innerStream, LZ4StreamMode.Compress, compressionFlags, blockSize);
	}

	/// <summary>Initializes a new instance of the <see cref="LZ4Stream" /> class.</summary>
	/// <param name="innerStream">The inner stream.</param>
	/// <param name="leaveOpen">Indicates if inner stream should be left open after decompression.</param>
	public static LZ4Stream Decode(Stream innerStream, bool leaveOpen = false)
	{
		var compressionFlags =
			leaveOpen ? LZ4StreamFlags.IsolateInnerStream : LZ4StreamFlags.None;
		return new LZ4Stream(innerStream, LZ4StreamMode.Decompress, compressionFlags);
	}

	/// <summary>Compresses and wraps given input byte buffer.</summary>
	/// <param name="inputBuffer">The input buffer.</param>
	/// <param name="inputOffset">The input offset.</param>
	/// <param name="inputLength">Length of the input.</param>
	/// <returns>Compressed buffer.</returns>
	/// <exception cref="System.ArgumentException">inputBuffer size of inputLength is invalid</exception>
	public static byte[] Wrap(
		byte[] inputBuffer, int inputOffset = 0, int inputLength = int.MaxValue) =>
		LZ4Wrapper.Wrap(inputBuffer, inputOffset, inputLength);

	/// <summary>Compresses (with high compression algorithm) and wraps given input byte buffer.</summary>
	/// <param name="inputBuffer">The input buffer.</param>
	/// <param name="inputOffset">The input offset.</param>
	/// <param name="inputLength">Length of the input.</param>
	/// <returns>Compressed buffer.</returns>
	/// <exception cref="System.ArgumentException">inputBuffer size of inputLength is invalid</exception>
	// ReSharper disable once InconsistentNaming
	public static byte[] WrapHC(
		byte[] inputBuffer, int inputOffset = 0, int inputLength = int.MaxValue) =>
		LZ4Wrapper.WrapHC(inputBuffer, inputOffset, inputLength);

	/// <summary>Unwraps the specified compressed buffer.</summary>
	/// <param name="inputBuffer">The input buffer.</param>
	/// <param name="inputOffset">The input offset.</param>
	/// <returns>Uncompressed buffer.</returns>
	/// <exception cref="System.ArgumentException">
	///     inputBuffer size is invalid or inputBuffer size is invalid or has been corrupted
	/// </exception>
	public static byte[] Unwrap(byte[] inputBuffer, int inputOffset = 0) =>
		LZ4Wrapper.Unwrap(inputBuffer, inputOffset);
}