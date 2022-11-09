using System;

namespace K4os.Compression.LZ4.Encoders;

/// <summary>
/// Interface of LZ4 decoder used by LZ4 streams.
/// </summary>
public interface ILZ4Decoder: IDisposable
{
	/// <summary>Block size.</summary>
	int BlockSize { get; }

	/// <summary>Bytes already decoded and available to be read.
	/// Always smaller than <see cref="BlockSize"/></summary>
	int BytesReady { get; }

	/// <summary>
	/// Decodes previously compressed block and caches decompressed block in decoder.
	/// Returns number of bytes decoded. These bytes can be read with <see cref="Drain" />.
	/// </summary>
	/// <param name="source">Points to compressed block.</param>
	/// <param name="length">Length of compressed block.</param>
	/// <param name="blockSize">Size of the block. Value <c>0</c> indicates default block size.</param>
	/// <returns>Number of decoded bytes.</returns>
	unsafe int Decode(byte* source, int length, int blockSize = 0);

	/// <summary>
	/// Inject already decompressed block and caches it in decoder.
	/// Used with uncompressed-yet-chained blocks and pre-made dictionaries.
	/// These bytes can be read with <see cref="Drain" />.
	/// </summary>
	/// <param name="source">Points to uncompressed block.</param>
	/// <param name="length">Length of uncompressed block.</param>
	/// <returns>Number of decoded bytes.</returns>
	unsafe int Inject(byte* source, int length);

	/// <summary>
	/// Reads previously decoded bytes. Please note, <paramref name="offset"/> should be
	/// negative number, pointing to bytes before current head. 
	/// </summary>
	/// <param name="target">Buffer to write to.</param>
	/// <param name="offset">Offset in source buffer relatively to current head.
	/// Please note, it should be negative value.</param>
	/// <param name="length">Number of bytes to read.</param>
	unsafe void Drain(byte* target, int offset, int length);
	
	/// <summary>
	/// Peeks at previously decoded bytes. Please note, <paramref name="offset"/> should be
	/// negative number, pointing to bytes before current head. 
	/// </summary>
	/// <param name="offset">Offset in source buffer relatively to current head.
	/// Please note, it should be negative value.</param>
	unsafe byte* Peek(int offset);
}