using K4os.Compression.LZ4.Encoders;
using K4os.Hash.xxHash;
using System.Buffers;
using System.Buffers.Binary;
using System.IO.Pipelines;

namespace K4os.Compression.LZ4.Buffers
{
    public sealed class Lz4FrameEncoder : IDisposable
    {
        private ILZ4Encoder? _encoder;
        private bool _encoderIsChained;
        private LZ4Level _encoderLevel;
        private byte[]? _buffer;
        private readonly XXH32 _contentChecksum = new();
        private readonly XXH32 _blockChecksum = new();

        /// <summary>
        /// Encode (compress) to a LZ4 frame.
        /// </summary>
        /// <param name="uncompressed">The source bytes to compress</param>
        /// <param name="compressed">The writer to where the compressed bytes are written</param>
        /// <param name="level">The compression level</param>
        /// <param name="frameDescriptor">The frame descriptor to use; if none is provided the default used is <see cref="LZ4FrameDescriptor.Default"/></param>
        /// <returns>The number of written bytes</returns>
        public int Encode(in ReadOnlySequence<byte> uncompressed, IBufferWriter<byte> compressed, LZ4Level level = LZ4Level.L00_FAST, LZ4FrameDescriptor? frameDescriptor = null)
        {
            var header = new LZ4FrameHeader(frameDescriptor ?? LZ4FrameDescriptor.Default);

            // Setup the encoder for parameters matching the header.
            Setup(header, level);

            // Write header
            var headerLength = header.Write(compressed.GetSpan(LZ4FrameHeader.MaxSize));
            compressed.Advance(headerLength);
            var totalWritten = headerLength;
            
            totalWritten += WriteBlocks(uncompressed, header, compressed);

            var block = FlushAndEncode();
            if (block.IsCompleted)
            {
                block = block with
                {
                    BlockChecksum = GetBlockChecksum(header)
                };
                totalWritten += WriteBlock(block, compressed);
            }

            // Write end mark
            totalWritten += WriteEndMark(header, compressed);

            return totalWritten;
        }

        /// <summary>
        /// Asynchronously encode (compress) to a LZ4 frame.
        /// </summary>
        /// <param name="uncompressed">The source bytes to compress</param>
        /// <param name="compressed">The writer to where the compressed bytes are written</param>
        /// <param name="level">The compression level</param>
        /// <param name="frameDescriptor">The frame descriptor to use; if none is provided the default used is <see cref="LZ4FrameDescriptor.Default"/></param>
        /// <param name="cancellationToken"></param>
        /// <returns>The number of written bytes</returns>
        public async ValueTask<int> EncodeAsync(PipeReader uncompressed, PipeWriter compressed, LZ4Level level = LZ4Level.L00_FAST, LZ4FrameDescriptor? frameDescriptor = null, CancellationToken cancellationToken = default)
        {
            var header = new LZ4FrameHeader(frameDescriptor ?? LZ4FrameDescriptor.Default);

            // Setup the encoder for parameters matching the header.
            Setup(header, level);

            // Write header
            var headerLength = header.Write(compressed.GetSpan(LZ4FrameHeader.MaxSize));
            compressed.Advance(headerLength);
            var totalWritten = headerLength;
            while (true)
            {
                var result = await uncompressed.ReadAsync(cancellationToken);
                var buffer = result.Buffer;

                var written = WriteBlocks(buffer, header, compressed);
                totalWritten += written;

                // All read bytes are consumed
                uncompressed.AdvanceTo(buffer.End);

                if (written > 0)
                {
                    // Signal that there are bytes ready
                    await compressed.FlushAsync(cancellationToken);
                }

                if (result.IsCompleted)
                {
                    var block = FlushAndEncode();
                    if (block.IsCompleted)
                    {
                        block = block with
                        {
                            BlockChecksum = GetBlockChecksum(header)
                        };
                        totalWritten += WriteBlock(block, compressed);
                    }

                    break;
                }
            }

            // Write end mark
            totalWritten += WriteEndMark(header, compressed);
            await compressed.FlushAsync(cancellationToken);

            return totalWritten;
        }

        private int WriteBlocks(in ReadOnlySequence<byte> source, LZ4FrameHeader header, IBufferWriter<byte> writer)
        {
            var totalWritten = 0;
            foreach (var segment in source)
            {
                var remaining = segment.Span;
                while (!remaining.IsEmpty)
                {
                    var block = TopupAndEncode(remaining, out var loaded);

                    UpdateBlockChecksum(header, remaining[..loaded]);

                    if (block.IsCompleted)
                    {
                        block = block with
                        {
                            BlockChecksum = GetBlockChecksum(header)
                        };
                        totalWritten += WriteBlock(block, writer);
                    }

                    remaining = remaining[loaded..];
                }

                if (header.FrameDescriptor.ContentChecksumFlag)
                {
                    _contentChecksum.Update(segment.Span);
                }
            }

            return totalWritten;
        }

        private void UpdateBlockChecksum(LZ4FrameHeader header, ReadOnlySpan<byte> blockContent)
        {
            if (!header.FrameDescriptor.BlockChecksumFlag)
            {
                return;
            }

            _blockChecksum.Update(blockContent);
        }

        private uint? GetBlockChecksum(LZ4FrameHeader header)
        {
            if (!header.FrameDescriptor.BlockChecksumFlag)
            {
                return null;
            }

            var digest = _blockChecksum.Digest();
            _blockChecksum.Reset();
            return digest;
        }

        private static int WriteBlock(in LZ4BlockInfo blockInfo, IBufferWriter<byte> writer)
        {
            var block = blockInfo.Span;
            var blockLength = block.Length;
            var span = writer.GetSpan(4 + blockLength);

            BinaryPrimitives.WriteUInt32LittleEndian(span, (uint)blockLength | (blockInfo.Compressed ? 0 : 0x80000000));
            block.CopyTo(span[4..]);
            writer.Advance(4 + blockLength);
            var written = 4 + blockLength;

            if (blockInfo.BlockChecksum.HasValue)
            {
                span = writer.GetSpan(4);
                BinaryPrimitives.WriteUInt32LittleEndian(span, blockInfo.BlockChecksum.Value);
                writer.Advance(4);
                written += 4;
            }

            return written;
        }

        private int WriteEndMark(LZ4FrameHeader header, IBufferWriter<byte> writer)
        {
            var span = writer.GetSpan(4);
            BinaryPrimitives.WriteUInt32LittleEndian(span, 0);
            writer.Advance(4);
            var totalWritten = 4;

            if (header.FrameDescriptor.ContentChecksumFlag)
            {
                // Content checksum
                span = writer.GetSpan(4);
                BinaryPrimitives.WriteUInt32LittleEndian(span, _contentChecksum.Digest());
                writer.Advance(4);
                totalWritten += 4;
            }

            return totalWritten;
        }

        private LZ4BlockInfo TopupAndEncode(ReadOnlySpan<byte> source, out int loaded)
        {
            var action = _encoder.TopupAndEncode(
                    source,
                    target: _buffer,
                    forceEncode: false,
                    allowCopy: true,
                    out loaded,
                    out var encoded);

            return new LZ4BlockInfo(_buffer!, encoded, action == EncoderAction.Encoded);
        }

        private LZ4BlockInfo FlushAndEncode()
        {
            var action = _encoder.FlushAndEncode(
                target: _buffer,
                allowCopy: true,
                out var encoded);

            return new LZ4BlockInfo(_buffer!, encoded, action == EncoderAction.Encoded);
        }

        private void Setup(LZ4FrameHeader header, LZ4Level level)
        {
            Reset();

            var chaining = !header.FrameDescriptor.BlockIndependenceFlag;
            var blockSize = header.FrameDescriptor.BlockMaximumSize;
            if (_encoder is null ||
                _encoderIsChained != chaining ||
                _encoderLevel != level ||
                _encoder.BlockSize != blockSize)
            {
                if (_encoder is not null)
                {
                    _encoder.Dispose();
                }

                _encoder = LZ4Encoder.Create(chaining, level, blockSize);
                _encoderIsChained = chaining;
                _encoderLevel = level;
            }

            var requiredBufferSize = LZ4Codec.MaximumOutputSize(header.FrameDescriptor.BlockMaximumSize);
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
            _blockChecksum.Reset();
        }

        public void Dispose()
        {
            Reset();

            if (_encoder is not null)
            {
                _encoder.Dispose();
                _encoder = null;
            }

            if (_buffer is not null)
            {
                ArrayPool<byte>.Shared.Return(_buffer);
                _buffer = null;
            }
        }
    }
}
