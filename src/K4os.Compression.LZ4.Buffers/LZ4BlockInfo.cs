using System;

namespace K4os.Compression.LZ4.Buffers
{
    internal readonly record struct LZ4BlockInfo(byte[] BlockBuffer, int BlockLength, bool Compressed, uint? BlockChecksum = null)
    {
        public Span<byte> Span => BlockBuffer.AsSpan(0, BlockLength);
        public bool IsCompleted => BlockLength != 0;
    }
}
