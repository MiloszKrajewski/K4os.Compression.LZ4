# K4os.Compression.LZ4

| Name | Nuget | Description |
|:-|:-:|:-|
| `K4os.Compression.LZ4`         | [![NuGet Stats](https://img.shields.io/nuget/v/K4os.Compression.LZ4.svg)](https://www.nuget.org/packages/K4os.Compression.LZ4) | Block compression only |
| `K4os.Compression.LZ4.Streams` | [![NuGet Stats](https://img.shields.io/nuget/v/K4os.Compression.LZ4.Streams.svg)](https://www.nuget.org/packages/K4os.Compression.LZ4.Streams) | Stream compression |

# LZ4

LZ4 is lossless compression algorithm, sacrificing compression ratio for compression/decompression speed. Its compression speed is ~400 MB/s per core while decompression speed reaches ~2 GB/s, not far from RAM speed limits.

This library brings LZ4 to .NET Standard compatible platforms: .NET Core, .NET Framework, Mono, Xamarin, and UWP. Well... theoretically. It is .NET Standard 1.6 so all this platforms should be supported although I did not test it on all this platforms.

Original LZ4 has been written by Yann Collet and original C sources can be found [here](https://github.com/Cyan4973/lz4)

# Build

```shell
paket restore
fake build
```

# What is 'Fast compression algorithm'?

While compression algorithms you use day-to-day to archive your data work around the speed of 10MB/s giving you quite decent compression ratios, 'fast algorithms' are designed to work 'faster than your hard drive' sacrificing compression ratio.
One of the most famous fast compression algorithms in Google's own [Snappy](http://code.google.com/p/snappy/) which is advertised as 250MB/s compression, 500MB/s decompression on i7 in 64-bit mode.
Fast compression algorithms help reduce network traffic / hard drive load compressing data on the fly with no noticeable latency.

I just tried to compress some sample data (Silesia Corpus) receiving:
* **zlib** (7zip) - 7.5M/s compression, 110MB/s decompression, 44% compression ratio
* **lzma** (7zip) - 1.5MB/s compression, 50MB/s decompression, 37% compression ratio
* **lz4** - 280MB/s compression, 520MB/s decompression, 57% compression ratio

**Note**: Values above are for illustration only. they are affected by HDD read/write speed (in fact LZ4 decompression in much faster). The 'real' tests are taking HDD speed out of equation. For detailed performance tests see [Performance Testing] and [Comparison to other algorithms].

## Other 'Fast compression algorithms'

There are multiple fast compression algorithms, to name a few: [LZO](http://lzohelper.codeplex.com/), [QuickLZ](http://www.quicklz.com/index.php), [LZF](http://csharplzfcompression.codeplex.com/), [Snappy](https://github.com/Kintaro/SnappySharp), FastLZ.
You can find comparison of them on [LZ4 webpage](http://code.google.com/p/lz4/) or [here](http://www.technofumbles.com/weblog/2011/04/22/survey-of-fast-compression-algorithms-part-1-2/)

# Usage

This LZ4 library can be used in two distinctive ways: to compress streams and blocks.

## Use as blocks

### Compression levels

```csharp
enum LZ4Level
{
    L00_FAST,
    L03_HC, L04_HC, L05_HC, L06_HC, L07_HC, L08_HC, L09_HC,
    L10_OPT, L11_OPT, L12_MAX,
}
```

There are multiple compression levels. LZ4 comes in 3 (4?) flavors of compression algorithms. You can notice suffixes of those levels: `FAST`, `HC`, `OPT` and `MAX` (while `MAX` is just `OPT` with "ultra" settings). Please note that compression speed drops rapidly when not using `FAST` mode, while decompression speed stays the same (actually, it is usually faster for high compression levels as there is less data to process).

### Utility

```csharp
static class LZ4Codec
{
    static int MaximumOutputSize(int length);
}
```

Returns maximum size of of a block after compression. Of course, most of the time compressed data will take less space than source data, although in case of incompressible (for example: already compressed) data it may take more.

Example:

```csharp
var source = new byte[1000];
var target = new[LZ4Codec.MaximumOutputSize(source.Length)];
//...
```

### Compression

Block can be compressed using `Encode(...)` method family. They are relatively low level functions as it is your job to allocate all memory.

```csharp
static class LZ4Codec
{
    static int Encode(
        byte* source, int sourceLength,
        byte* target, int targetLength,
        LZ4Level level = LZ4Level.L00_FAST);

    static int Encode(
        ReadOnlySpan<byte> source, Span<byte> target,
        LZ4Level level = LZ4Level.L00_FAST);

    static int Encode(
        byte[] source, int sourceOffset, int sourceLength,
        byte[] target, int targetOffset, int targetLength,
        LZ4Level level = LZ4Level.L00_FAST);
}
```

All of them compress `source` buffer into `target` buffer and return number of bytes actually used after compression. If this value is negative it means that error has occurred and compression failed. In most cases mean that `target` buffer is too small.

Please note, it might be tempting to use `target` buffer the same size (or even one byte smaller) then `source` buffer, and use copy as a fallback. This will work just fine, yet compression into buffer that is smaller than `MaximumOutputSize(source.Length)` is a little bit slower.

Example:

```csharp
var source = new byte[1000];
var target = new[LZ4Codec.MaximumOutputSize(source.Length)];
var encodedLength = LZ4Codec.Encode(
    source, 0, source.Length,
    target, 0, target.Length);
```

## Decompression

Previously compressed block can be decompressed with `Decode(...)` functions.

```csharp
static class LZ4Codec
{
    static int Decode(
        byte* source, int sourceLength,
        byte* target, int targetLength);

    static int Decode(
        ReadOnlySpan<byte> source, Span<byte> target);

    static int Decode(
        byte[] source, int sourceOffset, int sourceLength,
        byte[] target, int targetOffset, int targetLength);
}
```

You have to know upfront how much memory you need to decompress, as there is almost no way to guess it. I did not investigate theoretical maximum compression ration, yet all-zero buffer gets compressed 245 times, therefore when decompressing output buffer would need to be 245 times bigger than input buffer. Yet, encoding itself does not store that information anywhere therefore it is your job.

```csharp
var source = new byte[1000];
var target = new byte[knownOutputLength]; // or source.Length * 255 to be safe
var decoded = LZ4Codec.Decode(
    source, 0, source.Length,
    target, 0, target.Length);
```

## Pickler

Sometime all you need is too quickly compress a small chunk of data, let's say serialized message to send it over the network. You can use `LZ4Pickler` in such case. It does encode original length within a message and handles incompressible data (by copying).

```csharp
static class LZ4Pickler
{
    static byte[] Pickle(
        byte[] source,
        LZ4Level level = LZ4Level.L00_FAST);

    static byte[] Pickle(
        byte[] source, int sourceOffset, int sourceLength,
        LZ4Level level = LZ4Level.L00_FAST);

    static byte[] Pickle(
        ReadOnlySpan<byte> source,
        LZ4Level level = LZ4Level.L00_FAST);

    static byte[] Pickle(
        byte* source, int sourceLength,
        LZ4Level level = LZ4Level.L00_FAST);
}
```

Example:

```csharp
var source = new byte[1000];
var encoded = LZ4Pickler.Pickle(source);
var decoded = LZ4Pickler.Unpickle(encoded);
```

Please note that this approach is slightly slower (copy after failed compression) and has one extra memory allocation (as it resizes buffer after compression).

## Streams

Stream implementation is in different package (`K4os.Compression.LZ4.Streams`) as it has dependency on [`K4os.Hash.xxHash`](https://github.com/MiloszKrajewski/K4os.Hash.xxHash).
It is fully compatible with [LZ4 Frame format](https://github.com/lz4/lz4/blob/dev/doc/lz4_Frame_format.md) although not all features are supported on compression (they are "properly" ignored on decompression).

### Stream compression settings

There are some thing which can be configured when compressing data:

```csharp
class LZ4EncoderSettings
{
    long? ContentLength { get; set; } = null;
    bool ChainBlocks { get; set; } = true;
    int BlockSize { get; set; } = Mem.K64;
    bool ContentChecksum => false;
    bool BlockChecksum => false;
    uint? Dictionary => null;
    LZ4Level CompressionLevel { get; set; } = LZ4Level.L00_FAST;
    int ExtraMemory { get; set; } = 0;
}
```

Default options are good enough so you don't change anything. Refer to [original documentation](https://github.com/lz4/lz4/blob/dev/doc/lz4_Frame_format.md) for more detailed information.

Please note that `ContentLength`, `ContentChecksum`, `BlockChecksum` and `Dictionary` are not currently supported and trying to use values other than defaults will throw exceptions.

### Stream compression

The class responsible for compression is `LZ4EncoderStream` but its usage is not obvious. For easy access two [static factory methods](https://stackoverflow.com/questions/194496/static-factory-methods-vs-instance-normal-constructors) has been created:

```csharp
static class LZ4Stream
{
    static LZ4EncoderStream Encode(
        Stream stream, LZ4EncoderSettings settings = null, bool leaveOpen = false);

    static LZ4EncoderStream Encode(
        Stream stream, LZ4Level level, int extraMemory = 0,
        bool leaveOpen = false);
}
```

Both of them will take a stream (a file, a network stream, a memory stream) and wrap it adding compression on top of it.

Example:

```csharp
using (var source = File.OpenRead(filename))
using (var target = LZ4Stream.Encode(File.Create(filename + ".lz4")))
{
    source.CopyTo(target);
}
```

### Stream decompression settings

Decompression settings are pretty simple and class has been added for symmetry with `LZ4EncoderSettings`.

```csharp
class LZ4DecoderSettings
{
    int ExtraMemory { get; set; } = 0;
}
```

Adding extra memory to decompression process may increase decompression speed. Not significantly, though so there is no reason to stress about it too much.

### Stream decompression

Same as before, there are two static factory methods to wrap existing stream and provide decompression.

```csharp
static class LZ4Stream
{
    static LZ4DecoderStream Decode(
        Stream stream, LZ4DecoderSettings settings = null, bool leaveOpen = false);

    static LZ4DecoderStream Decode(
        Stream stream, int extraMemory, bool leaveOpen = false);
}
```

Example:

```csharp
using (var source = LZ4Stream.Decode(File.OpenRead(filename + ".lz4")))
using (var target = File.Create(filename))
{
    source.CopyTo(target);
}
```

Please note that stream decompression is (at least I hope it is) fully compatible with original specification. Well, it does not handle predefined dictionaries but `lz4.exe` does not either. All the other features which are not implemented yet (`ContentLength`, `ContentChecksum`, `BlockChecksum`) are just gracefully ignored but does not cause decompression to fail.
