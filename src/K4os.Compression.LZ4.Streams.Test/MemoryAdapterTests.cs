using System;
using System.IO;
using K4os.Compression.LZ4.Internal;
using K4os.Compression.LZ4.Streams.Abstractions;
using K4os.Compression.LZ4.Streams.Adapters;
using TestHelpers;
using Xunit;

namespace K4os.Compression.LZ4.Streams.Test;

public class MemoryAdapterTests
{
	private static readonly LZ4Descriptor DefaultSettings =
		new(null, false, true, false, null, Mem.K64);

	[Theory]
	[InlineData(".corpus/webster")]
	[InlineData(".corpus/mozilla")]
	[InlineData(".corpus/dickens")]
	public void RoundtripWithMemory(string filename)
	{
		filename = Tools.FindFile(filename);
		var originalBytes = File.ReadAllBytes(filename);

		var compressedBytes = new byte[LZ4Codec.MaximumOutputSize(originalBytes.Length)];

		var encoder = new FrameEncoder<MemoryAdapter, Memory<byte>>(
			new MemoryAdapter(),
			new Memory<byte>(compressedBytes),
			d => d.CreateEncoder(),
			DefaultSettings);

		WriteAllBytes(originalBytes, encoder);
		var compressedLength = compressedBytes.Length - encoder.Stream.Length;

		var decoder = new FrameDecoder<MemoryAdapter, ReadOnlyMemory<byte>>(
			new MemoryAdapter(),
			new ReadOnlyMemory<byte>(compressedBytes, 0, compressedLength),
			d => d.CreateDecoder());

		var decompressedBytes = ReadAllBytes(decoder);

		Tools.SameBytes(originalBytes.AsSpan(), decompressedBytes.Span);
	}

	[Theory]
	[InlineData(".corpus/webster")]
	[InlineData(".corpus/mozilla")]
	[InlineData(".corpus/dickens")]
	public void RoundtripWithBufferWriter(string filename)
	{
		filename = Tools.FindFile(filename);
		var originalBytes = File.ReadAllBytes(filename);

		var compressedBytes = new BufferWriter();

		var encoder = new FrameEncoder<BufferWriterAdapter<BufferWriter>, BufferWriter>(
			new BufferWriterAdapter<BufferWriter>(),
			compressedBytes,
			d => d.CreateEncoder(),
			DefaultSettings);

		WriteAllBytes(originalBytes, encoder);

		var decoder = new FrameDecoder<MemoryAdapter, ReadOnlyMemory<byte>>(
			new MemoryAdapter(),
			compressedBytes.WrittenMemory,
			d => d.CreateDecoder());

		var decompressedBytes = ReadAllBytes(decoder);

		Tools.SameBytes(originalBytes.AsSpan(), decompressedBytes.Span);
	}

	[Theory]
	[InlineData(".corpus/webster")]
	[InlineData(".corpus/mozilla")]
	[InlineData(".corpus/dickens")]
	public unsafe void RoundtripWithUnsafeSpan(string filename)
	{
		filename = Tools.FindFile(filename);
		var originalBytes = File.ReadAllBytes(filename);

		var compressedBytes = new byte[LZ4Codec.MaximumOutputSize(originalBytes.Length)];

		fixed (byte* compressedPtr = compressedBytes)
		{
			var encoder = new FrameEncoder<UnsafeByteSpanAdapter, UnsafeByteSpan>(
				new UnsafeByteSpanAdapter(),
				new UnsafeByteSpan(new UIntPtr(compressedPtr), compressedBytes.Length),
				d => d.CreateEncoder(),
				DefaultSettings);

			WriteAllBytes(originalBytes, encoder);
			var compressedLength = compressedBytes.Length - encoder.Stream.Length;

			var decoder = new FrameDecoder<UnsafeByteSpanAdapter, UnsafeByteSpan>(
				new UnsafeByteSpanAdapter(),
				new UnsafeByteSpan(new UIntPtr(compressedPtr), compressedLength),
				d => d.CreateDecoder());

			var decompressedBytes = ReadAllBytes(decoder);
			Tools.SameBytes(originalBytes.AsSpan(), decompressedBytes.Span);
		}
	}

	private static void WriteAllBytes(ReadOnlyMemory<byte> buffer, IFrameEncoder encoder)
	{
		while (true)
		{
			var chunk = Math.Min(buffer.Length, 4096);
			if (chunk <= 0) break;

			encoder.WriteManyBytes(buffer.Span.Slice(0, chunk));
			buffer = buffer.Slice(chunk);
		}

		encoder.CloseFrame();
	}

	private static ReadOnlyMemory<byte> ReadAllBytes(IFrameDecoder decoder)
	{
		const int chunk = 4096;
		var writer = BufferWriter.New();
		while (true)
		{
			var bytes = decoder.ReadManyBytes(writer.GetSpan(chunk));
			if (bytes <= 0) break;

			writer.Advance(bytes);
		}

		return writer.WrittenMemory;
	}
}
