using K4os.Compression.LZ4.Encoders;
using K4os.Hash.xxHash;
using System.Buffers;
using System.Buffers.Binary;
using System.Diagnostics.CodeAnalysis;
using System.IO.Pipelines;

namespace K4os.Compression.LZ4.Buffers
{
    public sealed class LZ4FrameDecoder : IDisposable
    {
        private ILZ4Decoder? _decoder;
        private bool _decoderIsChained;
        private byte[]? _buffer;
        private readonly XXH32 _blockChecksum = new();
        private readonly XXH32 _contentChecksum = new();

        /// <summary>
        /// Get the frame descriptor from a buffer of frame bytes.
        /// This is useful for example if <see cref="LZ4FrameDescriptor.ContentSize"/> is specified in the frame,
        /// then the frame descriptor can be read in advance to pre-allocate the size of the decompressed buffer.
        /// </summary>
        /// <param name="frame">The frame buffer</param>
        /// <returns>The frame descriptor</returns>
        /// <exception cref="InvalidDataException"></exception>
        public LZ4FrameDescriptor GetFrameDescriptor(in ReadOnlySequence<byte> frame)
        {
            if (!TryReadHeader(frame, out var header, out _))
            {
                throw new InvalidDataException("Unable to read frame header");
            }
            return header.FrameDescriptor;
        }

        /// <summary>
        /// Decode (decompress) a single LZ4 frame.
        /// </summary>
        /// <param name="compressed">The frame buffer</param>
        /// <param name="decompressed">The writer to where the decompressed bytes are written</param>
        /// <returns>The number of frame bytes consumed from <paramref name="compressed"/></returns>
        /// <exception cref="InvalidDataException"></exception>
        public int Decode(in ReadOnlySequence<byte> compressed, IBufferWriter<byte> decompressed)
        {
            var totalConsumed = 0;
            if (compressed.IsEmpty)
            {
                return totalConsumed;
            }

            if (!TryReadHeader(compressed, out var header, out var headerLength))
            {
                throw new InvalidDataException("Unable to read frame header");
            }
            totalConsumed += headerLength;
            var remaining = compressed.Slice(headerLength);

            // Setup the decoder for parameters matching the header.
            Setup(header);

            while (true)
            {
                if (!TryTopupAndDecode(decompressed, header, remaining, out var block, out var blockConsumed))
                {
                    throw new InvalidDataException("Unable to completely read block");
                }

                totalConsumed += blockConsumed;
                remaining = remaining.Slice(blockConsumed);

                if (block.BlockLength == 0)
                {
                    return totalConsumed;
                }
            }
        }

        /// <summary>
        /// Asynchronously decode (decompress) a single LZ4 frame.
        /// </summary>
        /// <param name="compressed">The reader from where the frame bytes are read</param>
        /// <param name="decompressed">The writer to where the decompressed bytes are written</param>
        /// <param name="cancellationToken"></param>
        /// <returns>The number of frame bytes consumed from <paramref name="compressed"/></returns>
        /// <exception cref="InvalidDataException"></exception>
        public async ValueTask<int> DecodeAsync(PipeReader compressed, PipeWriter decompressed, CancellationToken cancellationToken = default)
        {
            var totalConsumed = 0;
            LZ4FrameHeader? header;
            while (true)
            {
                var result = await compressed.ReadAsync(cancellationToken);
                if (result.IsCompleted && result.Buffer.IsEmpty)
                {
                    // It seems as if there is no header to be read, i.e. there are no (more) frames to read.
                    return totalConsumed;
                }

                if (!TryReadHeader(result.Buffer, out header, out var headerLength))
                {
                    // The entire header is not present - nothing is consumed, everying is examined.
                    compressed.AdvanceTo(result.Buffer.Start, result.Buffer.End);
                    continue;
                }

                // Consume the header
                totalConsumed += headerLength;
                compressed.AdvanceTo(result.Buffer.GetPosition(offset: headerLength));
                break;
            }

            // Setup the decoder for parameters matching the header.
            Setup(header);

            while (true)
            {
                var result = await compressed.ReadAsync(cancellationToken);
                if (!TryTopupAndDecode(decompressed, header, result.Buffer, out var block, out var blockConsumed))
                {
                    if (result.IsCompleted)
                    {
                        throw new InvalidDataException("Received end of compressed before terminating block was received");
                    }

                    // The entire block is not present - nothing is consumed, everything is examined.
                    compressed.AdvanceTo(result.Buffer.Start, result.Buffer.End);
                    continue;
                }

                // Consume the block
                totalConsumed += blockConsumed;
                compressed.AdvanceTo(result.Buffer.GetPosition(offset: blockConsumed));

                if (block.BlockLength == 0)
                {
                    return totalConsumed;
                }

                // Signal that the block is ready.
                await decompressed.FlushAsync(cancellationToken);
            }
        }

        private bool TryReadHeader(in ReadOnlySequence<byte> source, [MaybeNullWhen(false)] out LZ4FrameHeader header, out int consumed)
        {
            if (LZ4FrameHeader.TryRead(source.FirstSpan, out header, out var headerLength))
            {
                consumed = headerLength;
                return true;
            }
            Span<byte> buffer = stackalloc byte[LZ4FrameHeader.MaxSize];
            var length = 0;

            foreach (var segment in source)
            {
                var span = segment.Span;
                var chunk = Math.Min(span.Length, buffer.Length - length);
                span[..chunk].CopyTo(buffer[length..]);

                if (length > 0)
                {
                    var headerSpan = buffer[..(length + chunk)];
                    if (LZ4FrameHeader.TryRead(headerSpan, out header, out headerLength))
                    {
                        consumed = headerLength;
                        return true;
                    }
                }

                length += chunk;
            }

            consumed = 0;
            return false;
        }

        private bool TryTopupAndDecode(IBufferWriter<byte> writer, LZ4FrameHeader header, in ReadOnlySequence<byte> source, out LZ4BlockInfo block, out int consumed)
        {
            if (!TryReadBlock(source, header, out block, out consumed))
            {
                return false;
            }

            if (block.BlockLength == 0)
            {
                if (header.FrameDescriptor.ContentChecksumFlag)
                {
                    if (!TryReadUInt32(source.Slice(consumed), out var CC))
                    {
                        return false;
                    }
                    consumed += 4;

                    var digest = _contentChecksum.Digest();
                    if (CC != digest)
                    {
                        throw new InvalidDataException($"Found content checksum digest to be {digest} but expected {CC}");
                    }
                }

                return true;
            }

            Span<byte> decoded;

            if (block.Compressed)
            {
                var decodedBytes = _decoder.Decode(block.BlockBuffer, 0, block.BlockLength);
                var span = writer.GetSpan(decodedBytes);
                _decoder.Drain(span, -decodedBytes, decodedBytes);
                decoded = span[..decodedBytes];
            }
            else
            {
                var span = writer.GetSpan(block.BlockLength);
                block.Span.CopyTo(span);
                decoded = span[..block.BlockLength];
            }

            writer.Advance(decoded.Length);

            if (block.BlockChecksum.HasValue)
            {
                _blockChecksum.Reset();
                _blockChecksum.Update(decoded);

                var digest = _blockChecksum.Digest();
                if (digest != block.BlockChecksum)
                {
                    throw new InvalidDataException($"Found block digest to be {digest} but expected {block.BlockChecksum}");
                }
            }

            if (header.FrameDescriptor.ContentChecksumFlag)
            {
                _contentChecksum.Update(decoded);
            }

            return true;
        }

        private bool TryReadBlock(in ReadOnlySequence<byte> source, LZ4FrameHeader header, out LZ4BlockInfo value, out int consumed)
        {
            consumed = 0;

            if (!TryReadUInt32(source, out var blockLength))
            {
                value = default;
                return false;
            }
            consumed = 4;

            if (blockLength == 0)
            {
                value = new LZ4BlockInfo(Array.Empty<byte>(), 0, false);
                return true;
            }

            var compressed = (blockLength & 0x80000000) == 0;
            blockLength &= 0x7FFFFFFF;

            if (source.Length < 4 + blockLength)
            {
                value = default;
                return false;
            }

            source.Slice(4, blockLength).CopyTo(_buffer);
            consumed += (int)blockLength;

            uint? blockChecksum = null;
            if (header.FrameDescriptor.BlockChecksumFlag)
            {
                if (!TryReadUInt32(source.Slice(4 + blockLength), out var includedBlockChecksum))
                {
                    value = default;
                    return false;
                }
                blockChecksum = includedBlockChecksum;
                consumed += 4;
            }

            value = new LZ4BlockInfo(_buffer!, (int)blockLength, compressed, blockChecksum);
            return true;
        }

        private bool TryReadUInt32(in ReadOnlySequence<byte> source, out uint value)
        {
            if (source.Length < 4)
            {
                value = default;
                return false;
            }

            if (source.FirstSpan.Length >= 4)
            {
                value = BinaryPrimitives.ReadUInt32LittleEndian(source.FirstSpan);
                return true;
            }
            else
            {
                Span<byte> buffer = stackalloc byte[4];
                source.Slice(0, 4).CopyTo(buffer);
                value = BinaryPrimitives.ReadUInt32LittleEndian(buffer);
                return true;
            }
        }

        private void Setup(LZ4FrameHeader header)
        {
            Reset();

            var chaining = !header.FrameDescriptor.BlockIndependenceFlag;
            var blockSize = header.FrameDescriptor.BlockMaximumSize;
            if (_decoder is null ||
                _decoderIsChained != chaining ||
                _decoder.BlockSize != blockSize)
            {
                if (_decoder is not null)
                {
                    _decoder.Dispose();
                }

                _decoder = LZ4Decoder.Create(chaining, blockSize);
                _decoderIsChained = chaining;
            }

            var requiredBufferSize = header.FrameDescriptor.BlockMaximumSize;
            if (_buffer is null || _buffer.Length < requiredBufferSize)
            {
                if (_buffer is not null)
                {
                    ArrayPool<byte>.Shared.Return(_buffer);
                }

                _buffer = ArrayPool<byte>.Shared.Rent(requiredBufferSize);
            }
        }

        private void Reset()
        {
            _contentChecksum.Reset();
        }

        public void Dispose()
        {
            Reset();

            if (_decoder is not null)
            {
                _decoder.Dispose();
                _decoder = null;
            }

            if (_buffer is not null)
            {
                ArrayPool<byte>.Shared.Return(_buffer);
                _buffer = null;
            }
        }
    }
}
