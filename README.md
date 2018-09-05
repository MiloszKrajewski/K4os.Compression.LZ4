# K4os.Compression.LZ4

[![NuGet Stats](https://img.shields.io/nuget/v/K4os.Compression.LZ4.svg)](https://www.nuget.org/packages/K4os.Compression.LZ4)

# LZ4

LZ4 is lossless compression algorithm, sacrificing compression ratio for compression/decompression speed. Its compression speed is ~400 MB/s per core while decompression speed reaches ~2 GB/s, not far from RAM speed limits.

This library brings LZ4 to .NET Standard compatible platforms: .NET Core, .NET Framework, Mono, Xamarin, and UWP. Well... theoretically. It is .NET Standard 1.6 so all this platforms should be supported although I did not test it on all this platforms.

Original LZ4 has been written by Yann Collet and original C sources can be found [here](https://github.com/Cyan4973/lz4)

# Build

```shell
paket restore
fake build
```

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

There are multiple compression levels. LZ4 comes with 4 flavours of compression algorithms. You can notice suffixes of those levels: `FAST`, `HC`, `OPT` and `MAX` (while `MAX` is just `OPT` with "ultra" settings). Please note that compression speed drops rapidly when not using `FAST` mode, while decompressions speed stays the same (actually, it is usually faster for high compression levels as there is less data to process).

### Utility

```csharp
static class LZ4Codec {
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

Block can be compressed using `Encode(...)` method family. It is realtively low level level funtions as it is your job to allocate all memory.


```csharp
static class LZ4Codec {
    static int Encode(
        byte* source, int sourceLength,
        byte* target, int targetLength,
        LZ4Level level = LZ4Level.L00_FAST);

    static int Encode(
        byte[] source, int sourceIndex, int sourceLength,
        byte[] target, int targetIndex, int targetLength,
        LZ4Level level = LZ4Level.L00_FAST);
}
```

They both return number of bytes which were actually used after compression. If this value is negative it the error has occured which in most cases mean that `target` buffer is too small. Please note, it might be tempting to use target buffer the same size (or even one byte smaller) as source buffer, and use copy as a fallback. This will work yet compression into buffer that is smaller than `MaximumOutputSize(source.Length)` is slower.

There is one more method in `Encode(...)` family which does exactly that:

```csharp
static class LZ4Codec {
    static byte[] Encode(
        byte[] source, int sourceIndex, int sourceLength,
        LZ4Level level = LZ4Level.L00_FAST);
}
```

It was available in lz4net, so I've reimplented it in this library, but I have mixed feeling about it as it has some pitfalls and leaky abstractions. I suggest to use `Pickle` (see below) instead.

This method returns a potentially compressed buffer, but if it's size if equal to `sourceLength` it means that is was not actually compressed but just copied.


int Decode(
    byte* source, byte* target, int sourceLength, int targetLength);

int Decode(
    byte[] source, int sourceIndex, int sourceLength,
    byte[] target, int targetIndex, int targetLength);

byte[] Decode(
    byte[] source, int sourceIndex, int sourceLength,
    int targetLength);

byte[] Pickle(
    byte[] source, int sourceIndex, int sourceLength,
    LZ4Level level = LZ4Level.L00_FAST);

byte[] Pickle(
    byte[] source, LZ4Level level = LZ4Level.L00_FAST);

byte[] Unpickle(
    byte[] source, int sourceIndex, int sourceLength);

byte[] Unpickle(byte[] source);
```




## Use with streams

```csharp
LZ4DecoderStream Decode(Stream stream, int extraMemory = 0);

LZ4DecoderStream Decode(Stream stream, LZ4DecoderSettings settings);

LZ4EncoderStream Encode(Stream stream, LZ4EncoderSettings settings);

LZ4EncoderStream Encode(Stream stream, LZ4Level level = LZ4Level.L00_FAST, int extraMemory = 0);
```

Compressing streams follow decorator pattern: `LZ4Stream` is-a `Stream` and takes-a `Stream`. Let's start with some imports as text we are going to compress:

```csharp
using System;
using System.IO;
using LZ4;

const string LoremIpsum =
    "Lorem ipsum dolor sit amet, consectetur adipiscing elit. Nulla sit amet mauris diam. " +
    "Mauris mollis tristique sollicitudin. Nunc nec lectus nec ipsum pharetra venenatis. " +
    "Fusce et consequat massa, eu vestibulum erat. Proin in lectus a lacus fermentum viverra. " +
    "Aliquam vel tellus aliquam, eleifend justo ultrices, elementum elit. " +
    "Donec sed ullamcorper ex, ac sagittis ligula. Pellentesque vel risus lacus. " +
    "Proin aliquet lectus et tellus tristique, eget tristique magna placerat. " +
    "Maecenas ut ipsum efficitur, lobortis mauris at, bibendum libero. " +
    "Curabitur ultricies rutrum velit, eget blandit lorem facilisis sit amet. " +
    "Nunc dignissim nunc iaculis diam congue tincidunt. Suspendisse et massa urna. " +
    "Aliquam sagittis ornare nisl, quis feugiat justo eleifend iaculis. " +
    "Ut pulvinar id purus non convallis.";
```

Now, we can write this text to compressed stream:

```csharp
static void WriteToStream()
{
    using (var fileStream = new FileStream("lorem.lz4", FileMode.Create))
    using (var lz4Stream = new LZ4Stream(fileStream, LZ4StreamMode.Compress))
    using (var writer = new StreamWriter(lz4Stream))
    {
        for (var i = 0; i < 100; i++)
            writer.WriteLine(LoremIpsum);
    }
}
```

and read it back:

```csharp
static void ReadFromStream()
{
    using (var fileStream = new FileStream("lorem.lz4", FileMode.Open))
    using (var lz4Stream = new LZ4Stream(fileStream, LZ4StreamMode.Decompress))
    using (var reader = new StreamReader(lz4Stream))
    {
        string line;
        while ((line = reader.ReadLine()) != null)
            Console.WriteLine(line);
    }
}
```

`LZ4Stream` constructor requires inner stream and compression mode, plus takes some optional arguments, but their defaults are relatively sane:

```csharp
LZ4Stream(
    Stream innerStream,
    LZ4StreamMode compressionMode,
    LZ4StreamFlags compressionFlags = LZ4StreamFlags.Default,
    int blockSize = 1024*1024);
```

where:

```csharp
enum LZ4StreamMode {
    Compress,
    Decompress
};

[Flags] enum LZ4StreamFlags {
    None,
    InteractiveRead,
    HighCompression,
    IsolateInnerStream,
    Default = None
}
```

`compressionMode` configures `LZ4Stream` to either `Compress` or `Decompress`. `compressionFlags` is optional argument and allows to:

* use `HighCompression` mode, which provides better compression ratio for the price of performance. This is relevant on compression only.
* use `IsolateInnerStream` mode to leave inner stream open after disposing `LZ4Stream`.
* use `InteractiveRead` mode to read bytes as soon as they are available. This option may be useful when dealing with network stream, but not particularly useful with regular file streams.

`blockSize` is set 1MB by default but can be changed. Bigger `blockSize` allows better compression ratio, but uses more memory, stresses garbage collector and increases latency. It might be worth to experiment with it.

## Use with byte arrays

You can also compress byte arrays. It is useful when compressed chunks are relatively small and their size in known when compressing. `LZ4Codec.Wrap` compresses byte array and returns byte array:

```csharp
static string CompressBuffer()
{
    var text = Enumerable.Repeat(LoremIpsum, 5).Aggregate((a, b) => a + "\n" + b);

    var compressed = Convert.ToBase64String(
        LZ4Codec.Wrap(Encoding.UTF8.GetBytes(text)));

    return compressed;
}
```

In the example above we a little bit more, of course: first we concatenate multiple strings (`Enumerable.Repeat(...).Aggregate(...)`), then encode text as UTF8 (`Encoding.UTF8.GetBytes(...)`), then compress it (`LZ4Codec.Wrap(...)`) and then encode it with Base64 (`Convert.ToBase64String(...)`). On the end we have base64-encoded compressed string.

To decompress it we can use something like this:

```csharp
static string DecompressBuffer(string compressed)
{
    var lorems =
        Encoding.UTF8.GetString(
                LZ4Codec.Unwrap(Convert.FromBase64String(compressed)))
            .Split('\n');

    foreach (var lorem in lorems)
        Console.WriteLine(lorem);
}
```

Which is a reverse operation: decoding base64 string (`Convert.FromBase64String(...)`), decompression (`LZ4Codec.Unwrap(...)`), decoding UTF8 (`Encoding.UTF8.GetString(...)`) and splitting the string (`Split('\n')`).

## Compatibility

Both `LZ4Stream` and `LZ4Codec.Wrap` is not compatible with original LZ4. It is an outstanding task to implement compatible streaming protocol and, to be honest, it does not seem to be likely in nearest future, but...

If you want to do it yourself, you can. It requires a little bit more understanding though, so let's look at "low level" compression. Let's create some compressible data:

```charp
var inputBuffer =
    Encoding.UTF8.GetBytes(
        Enumerable.Repeat(LoremIpsum, 5).Aggregate((a, b) => a + "\n" + b));
```

we also need to allocate buffer for compressed data.
Please note, it might be actually more than input data (as not all data can be compressed):

```csharp
var inputLength = inputBuffer.Length;
var maximumLength = LZ4Codec.MaximumOutputLength(inputLength);
var outputBuffer = new byte[maximumLength];
```

Now, we can run actual compression:

```csharp
var outputLength = LZ4Codec.Encode(
    inputBuffer, 0, inputLength,
    outputBuffer, 0, maximumLength);
```

`Encode` method returns number of bytes which were actually used. It might be less or equal to `maximumLength`. It me be also `0` (or less) to indicate that compression failed. This happens when provided buffer is too small.

Buffer compressed this way can be decompressed with:

```csharp
LZ4Codec.Decode(
    inputBuffer, 0, inputLength,
    outputBuffer, 0, outputLength,
    true);
```

Last argument (`true`) indicates that we actually know output length. Alternatively we don't have to provide it, and use:

```csharp
var guessedOutputLength = inputLength * 10; // ???
var outputBuffer = new byte[guessedOutputLength];
var actualOutputLength = LZ4Codec.Decode(
    inputBuffer, 0, inputLength,
    outputBuffer, 0, guessedOutputLength);
```

but this will require guessing outputBuffer size (`guessedOutputLength`) which might be quite inefficient.

**Buffers compressed this way are fully compatible with original implementation if LZ4.**

Both `LZ4Stream` and `LZ4Codec.Wrap/Unwrap` use them internally.


