using System.Runtime.InteropServices;

namespace K4os.Compression.LZ4.Buffers.Test
{
    internal static class PolyfillExtensions
    {
#if NET5_0_OR_GREATER
        internal static int Read(this Stream stream, Memory<byte> buffer) => stream.Read(buffer.Span);
#else
        internal static int Read(this Stream stream, Memory<byte> buffer)
        {
            if (!MemoryMarshal.TryGetArray(buffer, out ArraySegment<byte> arraySegment))
            {
                throw new NotSupportedException();
            }

            return stream.Read(arraySegment.Array!, arraySegment.Offset, arraySegment.Count);
        }
#endif
    }
}
