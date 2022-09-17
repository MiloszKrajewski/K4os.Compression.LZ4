namespace K4os.Compression.LZ4.Buffers
{
    internal readonly record struct LZ4BlockInfo(ReadOnlyMemory<byte> Memory, bool Compressed, uint? BlockChecksum = null)
    {
        public ReadOnlySpan<byte> Span => Memory.Span;

        public bool IsCompleted => !Memory.IsEmpty;
    }
}
