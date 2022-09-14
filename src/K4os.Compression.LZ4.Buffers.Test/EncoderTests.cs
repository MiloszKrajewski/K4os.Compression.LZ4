using K4os.Compression.LZ4.Internal;
using System.Buffers;
using System.IO.Pipelines;

namespace K4os.Compression.LZ4.Buffers.Test
{
    public class EncoderTests
    {
        public static readonly IEnumerable<object[]> TestData = new List<(string ExpectedFilename, string SourceFilename, LZ4Level Level, LZ4FrameDescriptor FrameDescriptor)>()
        {
            ("hello.bin", "hello.bin.uncompressed.lz4", LZ4Level.L00_FAST, new LZ4FrameDescriptor() { BlockIndependenceFlag = true, ContentChecksumFlag = true, BlockMaximumSize = Mem.K64 }),
            ("hello.bin", "hello.bin.blockchecksum.lz4", LZ4Level.L00_FAST, new LZ4FrameDescriptor() { BlockIndependenceFlag = true, BlockChecksumFlag = true, ContentChecksumFlag = true, BlockMaximumSize = Mem.K64 }),
            ("hello.bin", "hello.bin.contentsize.lz4", LZ4Level.L00_FAST, new LZ4FrameDescriptor() { BlockIndependenceFlag = true, ContentChecksumFlag = true, BlockMaximumSize = Mem.K64, ContentSize = -1 }),

            // See https://github.com/MiloszKrajewski/K4os.Compression.LZ4/issues/73
            //("large.bin", "large.bin.c1.lz4", LZ4Level.L00_FAST, new LZ4FrameDescriptor() { ContentChecksumFlag = true }), // Generated with "lz4 -v -B4 -BD -1 large.bin large.bin.c1.lz4"
            //("large.bin", "large.bin.c3.lz4", LZ4Level.L03_HC, new LZ4FrameDescriptor() { ContentChecksumFlag = true }), // Generated with "lz4 -v -B4 -BD -3 large.bin large.bin.c3.lz4"
        }
            .Select(x => new object[] { x.ExpectedFilename, x.SourceFilename, x.Level, x.FrameDescriptor });

        [Theory]
        [MemberData(nameof(TestData))]
        public void CanEncode(string sourceFilename, string expectedFilename, LZ4Level level, LZ4FrameDescriptor frameDescriptor)
        {
            // Given
            var uncompressedBytes = Utils.GetAssetBytes(sourceFilename);
            var expected = Utils.GetAssetBytes(expectedFilename);
            using var encoder = new Lz4FrameEncoder();

            if (frameDescriptor.ContentSize == -1)
            {
                frameDescriptor.ContentSize = uncompressedBytes.Length;
            }

            var uncompressed = new ReadOnlySequence<byte>(uncompressedBytes);
            var compressed = new ArrayBufferWriter<byte>();

            // When
            var written = encoder.Encode(uncompressed, compressed, level, frameDescriptor);

            // Then
            Assert.Equal(expected.Length, written);
            Assert.Equal(expected, compressed.WrittenSpan.ToArray());
        }

        [Theory]
        [MemberData(nameof(TestData))]
        public void CanEncode_Fragmentized(string sourceFilename, string expectedFilename, LZ4Level level, LZ4FrameDescriptor frameDescriptor)
        {
            // Given
            var uncompressedBytes = Utils.GetAssetBytes(sourceFilename);
            var expected = Utils.GetAssetBytes(expectedFilename);
            using var encoder = new Lz4FrameEncoder();

            if (frameDescriptor.ContentSize == -1)
            {
                frameDescriptor.ContentSize = uncompressedBytes.Length;
            }

            var uncompressed = Utils.Fragmentize(uncompressedBytes);
            var compressed = new ArrayBufferWriter<byte>();

            // When
            var written = encoder.Encode(uncompressed, compressed, level, frameDescriptor);

            // Then
            Assert.Equal(expected.Length, written);
            Assert.Equal(expected, compressed.WrittenSpan.ToArray());
        }

        [Theory]
        [MemberData(nameof(TestData))]
        public async Task CanEncodeAsync(string sourceFilename, string expectedFilename, LZ4Level level, LZ4FrameDescriptor frameDescriptor)
        {
            // Given
            var uncompressedBytes = Utils.GetAssetBytes(sourceFilename);
            var expected = Utils.GetAssetBytes(expectedFilename);
            using var encoder = new Lz4FrameEncoder();

            if (frameDescriptor.ContentSize == -1)
            {
                frameDescriptor.ContentSize = uncompressedBytes.Length;
            }

            var uncompressedPipe = new Pipe();
            var compressedPipe = new Pipe();
            var compressed = new ArrayBufferWriter<byte>();

            // When
            var readTask = ReadUncompressedAsync(uncompressedPipe.Writer);
            var encodeTask = encoder.EncodeAsync(uncompressedPipe.Reader, compressedPipe.Writer, level, frameDescriptor).AsTask();
            var writeTask = Utils.WriteToAsync(compressedPipe.Reader, compressed).AsTask();

            async Task ReadUncompressedAsync(PipeWriter writer)
            {
                writer.Write(uncompressedBytes);
                await writer.FlushAsync();
                await writer.CompleteAsync();
            }

            // Then
            await Task.WhenAll(readTask, encodeTask);
            await compressedPipe.Writer.CompleteAsync();
            await writeTask;
            var written = await encodeTask;

            // Then
            Assert.Equal(expected.Length, written);
            Assert.Equal(expected, compressed.WrittenSpan.ToArray());
        }

        [Theory]
        [MemberData(nameof(TestData))]
        public async Task CanEncodeAsync_Fragmentized(string sourceFilename, string expectedFilename, LZ4Level level, LZ4FrameDescriptor frameDescriptor)
        {
            // Given
            var uncompressedBytes = Utils.GetAssetBytes(sourceFilename);
            var expected = Utils.GetAssetBytes(expectedFilename);
            using var encoder = new Lz4FrameEncoder();

            if (frameDescriptor.ContentSize == -1)
            {
                frameDescriptor.ContentSize = uncompressedBytes.Length;
            }

            var uncompressedPipe = new Pipe();
            var compressedPipe = new Pipe();
            var compressed = new ArrayBufferWriter<byte>();

            // When
            var readTask = ReadUncompressedAsync(uncompressedPipe.Writer);
            var encodeTask = encoder.EncodeAsync(uncompressedPipe.Reader, compressedPipe.Writer, level, frameDescriptor).AsTask();
            var writeTask = Utils.WriteToAsync(compressedPipe.Reader, compressed).AsTask();

            async Task ReadUncompressedAsync(PipeWriter writer)
            {
                for (var i = 0; i < uncompressedBytes.Length; i++)
                {
                    writer.Write(new[] { uncompressedBytes[i] });
                    await writer.FlushAsync();
                    await Task.Yield();
                }
                await writer.CompleteAsync();
            }

            // Then
            await Task.WhenAll(readTask, encodeTask);
            await compressedPipe.Writer.CompleteAsync();
            await writeTask;
            var written = await encodeTask;

            // Then
            Assert.Equal(expected.Length, written);
            Assert.Equal(expected, compressed.WrittenSpan.ToArray());
        }
    }
}
