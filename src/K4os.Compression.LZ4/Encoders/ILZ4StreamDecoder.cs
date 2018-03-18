namespace K4os.Compression.LZ4.Encoders
{
    public interface ILZ4StreamDecoder
    {
        int BlockSize { get; }
        int BytesReady { get; }
        
        /// <summary>
        /// Decodes previously compressed block and caches decompressed block in decoder.
        /// Returns number of bytes decoded. These bytes can be read with <see cref="Drain"/>.
        /// </summary>
        /// <param name="source">Points to compressed block.</param>
        /// <param name="length">Length of compressed block.</param>
        /// <returns>Number of decoded bytes.</returns>
        unsafe int Decode(byte* source, int length);
        
        /// <summary>
        /// Reads previously decoded bytes. Please note, <paramref name="offset"/> should be
        /// negative number, pointing to bytes before current head. 
        /// </summary>
        /// <param name="target">Buffer to write to.</param>
        /// <param name="offset">Offset in source buffer relatively to current head</param>
        /// <param name="length">Number of bytes to read.</param>
        unsafe void Drain(byte* target, int offset, int length);
    }
}
