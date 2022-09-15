#if !NET5_0_OR_GREATER

using Nerdbank.Streams;
using System.Buffers;

namespace K4os.Compression.LZ4.Buffers.Test
{
    internal class ArrayBufferWriter<T> : IBufferWriter<T>
    {
        private Sequence<T> _sequence = new();

        public ReadOnlySpan<T> WrittenSpan => _sequence.AsReadOnlySequence.IsSingleSegment ? _sequence.AsReadOnlySequence.First.Span : _sequence.AsReadOnlySequence.ToArray();

        public void Advance(int count) => _sequence.Advance(count);

        public Memory<T> GetMemory(int sizeHint = 0) => _sequence.GetMemory(sizeHint);

        public Span<T> GetSpan(int sizeHint = 0) => _sequence.GetSpan(sizeHint);
    }
}

#endif
