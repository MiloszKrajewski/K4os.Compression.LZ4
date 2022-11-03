using System.Buffers;
using System.IO.Pipelines;

namespace K4os.Compression.LZ4.Buffers.Test
{
    public class DecoderTests
    {
        public static readonly IEnumerable<object[]> TestData = new List<(string UncompressedFilename, string CompressedFilename)>()
        {
            ("hello.bin", "hello.bin.uncompressed.lz4"),
            ("hello.bin", "hello.bin.compressed.lz4"),
            ("hello.bin", "hello.bin.blockchecksum.lz4"),
            ("hello.bin", "hello.bin.contentsize.lz4"),
            ("large.bin", "large.bin.c1.lz4"), // Generated with "lz4 -v -B4 -BD -1 large.bin large.bin.c1.lz4"
            ("large.bin", "large.bin.c3.lz4"), // Generated with "lz4 -v -B4 -BD -3 large.bin large.bin.c3.lz4"
        }
            .Select(x => new object[] { x.UncompressedFilename, x.CompressedFilename });

        [Theory]
        [MemberData(nameof(TestData))]
        public void CanDecode(string expectedFilename, string sourceFilename)
        {
            // Given
            var compressedBytes = Utils.GetAssetBytes(sourceFilename);
            var expected = Utils.GetAssetBytes(expectedFilename);
            using var decoder = new LZ4FrameDecoder();

            var compressed = new ReadOnlySequence<byte>(compressedBytes);
            var decompressed = new ArrayBufferWriter<byte>();

            // When
            var consumed = decoder.Decode(compressed, decompressed);

            // Then
            Assert.Equal(compressedBytes.Length, consumed);
            Assert.Equal(expected, decompressed.WrittenSpan.ToArray());
        }

        [Theory]
        [MemberData(nameof(TestData))]
        public void CanDecode_Fragmentized(string expectedFilename, string sourceFilename)
        {
            // Given
            var compressedBytes = Utils.GetAssetBytes(sourceFilename);
            var expected = Utils.GetAssetBytes(expectedFilename);
            using var decoder = new LZ4FrameDecoder();

            var compressedFragmented = Utils.Fragmentize(compressedBytes);
            var decompressed = new ArrayBufferWriter<byte>();

            // When
            var consumed = decoder.Decode(compressedFragmented, decompressed);

            // Then
            Assert.Equal(compressedBytes.Length, consumed);
            Assert.Equal(expected, decompressed.WrittenSpan.ToArray());
        }

        [Theory]
        [MemberData(nameof(TestData))]
        public async Task CanDecodeAsync(string expectedFilename, string sourceFilename)
        {
            // Given
            var compressedBytes = Utils.GetAssetBytes(sourceFilename);
            var expected = Utils.GetAssetBytes(expectedFilename);
            using var decoder = new LZ4FrameDecoder();

            var compressedPipe = new Pipe();
            var decompressedPipe = new Pipe();
            var decompressed = new ArrayBufferWriter<byte>();

            // When
            var readTask = ReadCompressedAsync(compressedPipe.Writer);
            var decodeTask = decoder.DecodeAsync(compressedPipe.Reader, decompressedPipe.Writer).AsTask();
            var writeTask = Utils.WriteToAsync(decompressedPipe.Reader, decompressed).AsTask();

            async Task ReadCompressedAsync(PipeWriter writer)
            {
                writer.Write(compressedBytes);
                await writer.FlushAsync();
            }

            await Task.WhenAll(readTask, decodeTask);
            await decompressedPipe.Writer.CompleteAsync();
            await writeTask;
            var consumed = await decodeTask;

            // Then
            Assert.Equal(compressedBytes.Length, consumed);
            Assert.Equal(expected, decompressed.WrittenSpan.ToArray());
        }

        [Theory]
        [MemberData(nameof(TestData))]
        public async Task CanDecodeAsync_Fragmentized(string expectedFilename, string sourceFilename)
        {
            // Given
            var compressedBytes = Utils.GetAssetBytes(sourceFilename);
            var expected = Utils.GetAssetBytes(expectedFilename);
            using var decoder = new LZ4FrameDecoder();

            var compressedPipe = new Pipe();
            var decompressedPipe = new Pipe();
            var decompressed = new ArrayBufferWriter<byte>();

            // When
            var readTask = ReadCompressedAsync(compressedPipe.Writer);
            var decodeTask = decoder.DecodeAsync(compressedPipe.Reader, decompressedPipe.Writer).AsTask();
            var writeTask = Utils.WriteToAsync(decompressedPipe.Reader, decompressed).AsTask();

            async Task ReadCompressedAsync(PipeWriter writer)
            {
                for (var i = 0; i < compressedBytes.Length; i++)
                {
                    writer.Write(new[] { compressedBytes[i] });
                    await writer.FlushAsync();
                    await Task.Yield();
                }
            }

            await Task.WhenAll(readTask, decodeTask);
            await decompressedPipe.Writer.CompleteAsync();
            await writeTask;
            var consumed = await decodeTask;

            // Then
            Assert.Equal(compressedBytes.Length, consumed);
            Assert.Equal(expected, decompressed.WrittenSpan.ToArray());
        }
    }
}
