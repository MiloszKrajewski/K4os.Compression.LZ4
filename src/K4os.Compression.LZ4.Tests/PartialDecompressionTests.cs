using System;
using TestHelpers;
using Xunit;

namespace K4os.Compression.LZ4.Tests;

public class PartialDecompressionTests
{
    [Theory]
    // Full Decompress
    [InlineData(127, 127)] // prime
    [InlineData(128, 128)]
    [InlineData(256, 256)]
    // Prime Sizes
    [InlineData(512, 17)] // One prime, one power of two
    [InlineData(511, 13)] // Both prime
    [InlineData(511, 31)] // Both prime
    public void CanDecodePartialCompressedBufferWithSpan(int sourceLength, int numToDecompress)
    {
        var targetLength = LZ4Codec.MaximumOutputSize(sourceLength);
        var source = new byte[sourceLength];
        var encoded = new byte[targetLength];
        var decoded = new byte[sourceLength];

        Lorem.Fill(source, 0, source.Length);
        Fill(encoded, 0xCD);
        Fill(decoded, 0xCD);

        // Encode a full file.
        var encodedLength = LZ4Codec.Encode(
            source.AsSpan(),
            encoded.AsSpan());

        // Verify data after encodedLength was not overwritten.
        Check(encoded, encodedLength, encoded.Length - encodedLength, 0xCD);

        // Partially decode the file.
        var decodedLength = LZ4Codec.PartialDecode(
            encoded.AsSpan(0, encodedLength),
            decoded.AsSpan(0, numToDecompress));

        // Verify expected length was decompressed.
        Assert.Equal(numToDecompress, decodedLength);
        Check(decoded, 0, decodedLength, source); // Verify correct data was decoded.
        Check(decoded, decodedLength, sourceLength - decodedLength, 0xCD); // Verify remaining padding is correct.
    }

    private static void Fill(byte[] buffer, byte value) => buffer.AsSpan().Fill(value); // vectorised on newer runtimes

    private void Check(byte[] buffer, int offset, int length, byte value)
    {
        for (var i = offset; i < offset + length; i++)
            if (buffer[i] != value)
                Assert.Fail($"Value overriden @ {i}");
    }

    private void Check(byte[] buffer, int offset, int length, byte[] source)
    {
        for (var i = offset; i < offset + length; i++)
            if (buffer[i] != source[i])
                Assert.Fail($"Value different @ {i}");
    }
}