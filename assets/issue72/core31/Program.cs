using System;
using System.Buffers;
using K4os.Compression.LZ4.Streams;

var bytes = new byte[65536];
var chunk = new byte[256];
var random = new Random(0);
random.NextBytes(chunk);
for (var i = 0; i < bytes.Length; i += chunk.Length)
	Buffer.BlockCopy(chunk, 0, bytes, i, chunk.Length);
var encoded = LZ4Frame.Encode(bytes.AsSpan(), new ArrayBufferWriter<byte>());
var ratio = (double)encoded.WrittenMemory.Length / bytes.Length;

if (ratio > 1)
	throw new Exception("Compression failed");

Console.WriteLine("Compression ratio: {0:P2}", ratio);
