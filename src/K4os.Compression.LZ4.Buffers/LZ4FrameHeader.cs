﻿using System.Buffers.Binary;
using System.Diagnostics.CodeAnalysis;

namespace K4os.Compression.LZ4.Buffers
{
    public sealed class LZ4FrameHeader
    {
        public const int MaxSize = 4 + LZ4FrameDescriptor.MaxSize;
        private const uint Magic = 0x184D2204;

        public LZ4FrameDescriptor FrameDescriptor { get; }

        internal LZ4FrameHeader(LZ4FrameDescriptor frameDescriptor)
        {
            FrameDescriptor = frameDescriptor;
        }

        internal static bool TryRead(ReadOnlySpan<byte> source, [MaybeNullWhen(false)] out LZ4FrameHeader result, out int length)
        {
            if (!BinaryPrimitives.TryReadUInt32LittleEndian(source, out var magic))
            {
                result = null;
                length = 0;
                return false;
            }

            if (magic != Magic)
            {
                throw new InvalidDataException("Invalid LZ4 frame magic");
            }

            if (!LZ4FrameDescriptor.TryRead(source.Slice(4), out var frameDescriptor, out var frameDescriptorLength))
            {
                result = null;
                length = 0;
                return false;
            }

            result = new LZ4FrameHeader(frameDescriptor);
            length = 4 + frameDescriptorLength;
            return true;
        }

        internal int Write(Span<byte> destination)
        {
            // Magic
            BinaryPrimitives.WriteUInt32LittleEndian(destination, Magic);

            // Frame Descriptor
            var fdLength = FrameDescriptor.Write(destination.Slice(4));

            return 4 + fdLength;
        }
    }
}
