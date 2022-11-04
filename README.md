# K4os.Compression.LZ4

| Name | Nuget | Description |
|:-|:-:|:-|
| `K4os.Compression.LZ4`         | [![NuGet Stats](https://img.shields.io/nuget/v/K4os.Compression.LZ4.svg)](https://www.nuget.org/packages/K4os.Compression.LZ4)                 | Block compression only |
| `K4os.Compression.LZ4.Streams` | [![NuGet Stats](https://img.shields.io/nuget/v/K4os.Compression.LZ4.Streams.svg)](https://www.nuget.org/packages/K4os.Compression.LZ4.Streams) | Stream compression     |
| `K4os.Compression.LZ4.Legacy`  | [![NuGet Stats](https://img.shields.io/nuget/v/K4os.Compression.LZ4.Legacy.svg)](https://www.nuget.org/packages/K4os.Compression.LZ4.Legacy)   | Legacy compatibility   |

# LZ4

LZ4 is lossless compression algorithm, sacrificing compression ratio for compression/decompression speed. Its compression speed is ~400 MB/s per core while decompression speed reaches ~2 GB/s, not far from RAM speed limits.

This library brings LZ4 to .NET Standard compatible platforms: .NET Core, .NET Framework, Mono, Xamarin, and UWP. Well... theoretically. It is .NET Standard 1.6 so all this platforms should be supported although I did not test it on all this platforms.

LZ4 has been written by Yann Collet and original C sources can be found [here](https://github.com/Cyan4973/lz4)

# Build

```shell
paket restore
fake build
```

# Changes

Change log can be found [here](CHANGES.md).

# Support

Maintaining this library is outside of my daily job completely. Company I work for is not even using it, so I do this completely in my own free time.

So, if you think my work is worth something, you could support me by funding my daily caffeine dose:

[!["Buy Me A Coffee"](https://www.buymeacoffee.com/assets/img/custom_images/orange_img.png)](https://buymeacoffee.com/miloszkrajewski)

(or just use [PayPal](https://www.paypal.com/donate?hosted_button_id=P3MJXJHG7NFE2))

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
var target = new byte[LZ4Codec.MaximumOutputSize(source.Length)];
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
var target = new byte[LZ4Codec.MaximumOutputSize(source.Length)];
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

You have to know upfront how much memory you need to decompress, as there is almost no way to guess it. I did not investigate theoretical maximum compression ratio, yet all-zero buffer gets compressed 245 times, therefore when decompressing output buffer would need to be 245 times bigger than input buffer. Yet, encoding itself does not store that information anywhere therefore it is your job.

```csharp
var source = new byte[1000];
var target = new byte[knownOutputLength]; // or source.Length * 255 to be safe
var decoded = LZ4Codec.Decode(
    source, 0, source.Length,
    target, 0, target.Length);
```

**NOTE:** If I told you that decompression needs potentially 100 times more memory than original data you would think this is insane. And it is not 100 times, it is 255 times more, so it actually is insane. Please don't do it. This was for demonstration only. What you need is a way to store original size somehow (I'm not opinionated, do whatever you think is right) or... you can use `LZ4Pickler` (see below) or `LZ4Stream`.

## Pickler

Sometimes all you need is to quickly compress a small chunk of data, let's say serialized message to send it over the network. You can use `LZ4Pickler` in such case. It does encode original length within a message and handles incompressible data (by copying).

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

Default options are good enough so you don't change anything. 
Refer to [original documentation](https://github.com/lz4/lz4/blob/dev/doc/lz4_Frame_format.md) 
for more detailed information.

Please note that `ContentLength`, `ContentChecksum`, `BlockChecksum` and `Dictionary` are not currently 
supported and trying to use values other than defaults will throw exceptions.

### Stream compression

The class responsible for compression is `LZ4EncoderStream` but its usage is not obvious. 
For easy access two [static factory methods](https://stackoverflow.com/questions/194496/static-factory-methods-vs-instance-normal-constructors) 
has been created:

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

Adding extra memory to decompression process may increase decompression speed. Not significantly, 
though so there is no reason to stress about it too much.

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

Please note that stream decompression is (at least I hope it is) fully compatible with original specification. 
Well, it does not handle predefined dictionaries but `lz4.exe` does not either. All the other features which are 
not implemented yet (`ContentLength`, `ContentChecksum`, `BlockChecksum`) are just gracefully ignored but does 
not cause decompression to fail.

### Other stream-like data structures

As per version 1.3-beta new stream abstractions has been added (note, it has both sync and async methods, but here I'm listing sync ones only):

```csharp
interface ILZ4FrameReader: IDisposable
{
	bool OpenFrame();
	long? GetFrameLength();
	int ReadOneByte();
	int ReadManyBytes(Span<byte> buffer, bool interactive = false);
	long GetBytesRead();
	void CloseFrame();
}

interface ILZ4FrameWriter: IDisposable
{
	bool OpenFrame();
	void WriteOneByte(byte value);
	void WriteManyBytes(ReadOnlySpan<byte> buffer);
	long GetBytesWritten();
	void CloseFrame();
}
```

This allows to adapt any stream-like data structure to LZ4 compression/decompression, which currently is:
`Span` and `ReadOnlySpan` (limited support), `Memory` and `ReadOnlyMemory`, `ReadOnlySequence`, `BufferWriter`, 
`Stream`, `PipeReader`, and `PipeWriter`.

This mechanism is extendable so implementing stream-like approach for other data structures will be possible (although not trivial).

Factory methods for creating `ILZ4FrameReader` and `ILZ4FrameWriter` are available on `LZFrame` class:

```csharp
static class LZ4Frame
{
    // Decode
    
	static void Decode<TBufferWriter>(
		ReadOnlySpan<byte> source, TBufferWriter target, int extraMemory = 0);
	static ByteMemoryLZ4FrameReader Decode(
		ReadOnlyMemory<byte> memory, int extraMemory = 0);
	static ByteSequenceLZ4FrameReader Decode(
		ReadOnlySequence<byte> sequence, int extraMemory = 0);
	static StreamLZ4FrameReader Decode(
		Stream stream, int extraMemory = 0, bool leaveOpen = false);
	static PipeLZ4FrameReader Decode(
		PipeReader reader, int extraMemory = 0, bool leaveOpen = false);
	
	// Encode
		
	static int Encode(
		ReadOnlySequence<byte> source, Span<byte> target, LZ4EncoderSettings? settings = default);
	static int Encode(
		Span<byte> source, Span<byte> target, LZ4EncoderSettings? settings = default);
	static int Encode(
		Action<ILZ4FrameWriter> source, Span<byte> target, LZ4EncoderSettings? settings = default);
	static ByteSpanLZ4FrameWriter Encode(
		byte* target, int length, LZ4EncoderSettings? settings = default);
	static ByteMemoryLZ4FrameWriter Encode(
		Memory<byte> target, LZ4EncoderSettings? settings = default);
	static ByteBufferLZ4FrameWriter<TBufferWriter> Encode<TBufferWriter>(
		TBufferWriter target, LZ4EncoderSettings? settings = default);
	static ByteBufferLZ4FrameWriter Encode(
		IBufferWriter<byte> target, LZ4EncoderSettings? settings = default);
	static StreamLZ4FrameWriter Encode(
		Stream target, LZ4EncoderSettings? settings = default, bool leaveOpen = false);
	static PipeLZ4FrameWriter Encode(
		PipeWriter target, LZ4EncoderSettings? settings = default, bool leaveOpen = false);
}
```

### Performance for small frames

Lot of LZ4 usage are small frames, like network packets, or small files. As this is still work not finished,
and there is more memory allocation than I would like, performance is getting much better.

Please note, `LZPickler` is the fastest option, it is just not portable, as it has been developed by me, for my own needs.

If you need to use `LZ4Frame` format (the official streaming format) you can find with new abstraction it can be much faster.
So far, people needed to use `Stream` even if data was in memory already:

```csharp
using var source = new MemoryStream(_encoded);
using var decoder = LZ4Stream.Decode(source);
using var target = new MemoryStream();
decoder.CopyTo(target);
_decoded = target.ToArray();
```

Not it is simpler, and faster:

```csharp
_decode = LZ4Frame.Decode(_encoded.AsSpan(), new ArrayBufferWriter<byte>()).WrittenMemory.ToArray();
```

[`ArrayBufferWriter<T>`](https://learn.microsoft.com/en-us/dotnet/api/system.buffers.arraybufferwriter-1?view=net-6.0) 
is a go to implementation of `IBufferWriter<T>`, but you may want specialized implementation, if performance is critical.
It is quite fast, but it seems is relatively relaxed about allocating memory. 
If you want to reduce GC usage, implement your own `IBufferWriter<T>` and test it.

Below decoding small blocks:

```csharp
using var source = new MemoryStream(_encoded);
using var decoder = LZ4Stream.Decode(source);
_ = decoder.Read(_decoded, 0, _decoded.Length);
```

vs

```csharp
using var decoder = LZ4Frame.Decode(_encoded);
_ = decoder.ReadManyBytes(_decoded.AsSpan());
```

shows that frame reader is much faster, and is allocating less memory, also please note no Gen1 or Get2 allocations.

```
BenchmarkDotNet=v0.13.2, OS=Windows 10 (10.0.19044.1889/21H2/November2021Update)
AMD Ryzen 5 3600, 1 CPU, 12 logical and 6 physical cores
.NET SDK=6.0.300
  [Host]     : .NET 5.0.17 (5.0.1722.21314), X64 RyuJIT AVX2
  DefaultJob : .NET 5.0.17 (5.0.1722.21314), X64 RyuJIT AVX2


|         Method | Size |       Mean |    Error |   StdDev | Ratio |   Gen0 |   Gen1 |   Gen2 | Allocated | Alloc Ratio |
|--------------- |----- |-----------:|---------:|---------:|------:|-------:|-------:|-------:|----------:|------------:|
|      UseStream |  128 | 1,173.6 ns | 23.23 ns | 52.44 ns |  1.00 | 1.2703 | 1.2074 | 1.2074 |     517 B |        1.00 |
| UseFrameReader |  128 |   466.2 ns |  2.00 ns |  1.67 ns |  0.40 | 0.0525 |      - |      - |     440 B |        0.85 |
|                |      |            |          |          |       |        |        |        |           |             |
|      UseStream | 1024 | 1,593.2 ns | 20.90 ns | 19.55 ns |  1.00 | 1.6575 | 1.5945 | 1.5945 |     518 B |        1.00 |
| UseFrameReader | 1024 |   756.7 ns |  7.08 ns |  6.27 ns |  0.47 | 0.0525 |      - |      - |     440 B |        0.85 |
|                |      |            |          |          |       |        |        |        |           |             |
|      UseStream | 8192 | 5,081.0 ns | 43.91 ns | 38.92 ns |  1.00 | 5.1956 | 5.1270 | 5.1270 |     535 B |        1.00 |
| UseFrameReader | 8192 | 3,836.3 ns | 12.11 ns | 11.33 ns |  0.76 | 0.0458 |      - |      - |     440 B |        0.82 |
```

### Legacy (lz4net) compatibility

There is a separate package for those who used lz4net before and still need to access files generated with it:

```csharp
static class LZ4Legacy
{
    static LZ4Stream Encode(
        Stream innerStream,
        bool highCompression = false,
        int blockSize = 1024 * 1024,
        bool leaveOpen = false);

    static LZ4Stream Decode(
        Stream innerStream, 
        bool leaveOpen = false);
}
```

This provide access to streams written by `lz4net`. Please note, that interface is not compatible, but the file format is.

Example:

```csharp
using (var source = LZ4Legacy.Decode(File.OpenRead(filename + ".old")))
using (var target = LZ4Stream.Encode(File.Create(filename + ".new")))
{
    source.CopyTo(target);
}
```

Code above will read old (lz4net) format and write to new format 
([lz4_Frame_format](https://github.com/lz4/lz4/blob/dev/doc/lz4_Frame_format.md)).

### Memory pooling

I've added memory block pooling to most of the classes. It is enabled by default, but comes with potential danger: 
this pooled memory gets pinned and may be problematic in some scenarios, for example with long lived streams.

LZ4 is used most of the time for small packages, "in-and-out 20 minutes adventure", so pinning pooling memory is not a problem. 

As usual: it depends.

If you want to change the maximum pooled array use `PinnedMemory.MaxPooledSize`. You can set it to 0 to disable pooling.  

### ARMv7, IL2CPP, Unity

Apparently ARMv7 does not handle unaligned access:

> [...] It looks like the code here will do an unaligned memory access, which is not allowed on armv7, hence 
> the crash. Mono works in this case because it generates code that is less efficient than IL2CPP, and does 
> only aligned memory access. With IL2CPP, we have chosen to convert the C# code as-is, so that the generated 
> code will do unaligned access if the C# code does. [...]

I've adapted 32-bit algorithm to use aligned access only (64-bit version still tries to maximise speed by 
using unaligned access). Think about 32-bit as "compatibility mode". 
It case of alignment related problems force 32-bit mode as soon as possible with:

```csharp
LZ4Codec.Enforce32 = true;
```

### Issues

Please use [this template](https://github.com/MiloszKrajewski/K4os.Compression.LZ4/issues/new?template=bug_report.md) when raising one. 
Try to be as helpful as possible to help me reproduce it.
