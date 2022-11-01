using System;
using System.Buffers;
using System.IO;
using K4os.Compression.LZ4.Internal;
using K4os.Compression.LZ4.Streams.Abstractions;
using K4os.Compression.LZ4.Streams.Adapters;
using K4os.Compression.LZ4.Streams.Frames;
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

		var encoder = new LZ4FrameWriter<ByteMemoryWriteAdapter, int>(
			new ByteMemoryWriteAdapter(new Memory<byte>(compressedBytes)),
			0,
			d => d.CreateEncoder(),
			DefaultSettings);

		WriteAllBytes(originalBytes, encoder);
		var compressedLength = encoder.StreamState;

		var decoder = new LZ4FrameReader<ByteMemoryReadAdapter, int>(
			new ByteMemoryReadAdapter(
				new ReadOnlyMemory<byte>(compressedBytes, 0, compressedLength)),
			0,
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

		var encoder = new LZ4FrameWriter<ByteBufferAdapter<BufferWriter>, BufferWriter>(
			new ByteBufferAdapter<BufferWriter>(),
			compressedBytes,
			d => d.CreateEncoder(),
			DefaultSettings);

		WriteAllBytes(originalBytes, encoder);

		var decoder = new LZ4FrameReader<ByteMemoryReadAdapter, int>(
			new ByteMemoryReadAdapter(compressedBytes.WrittenMemory),
			0,
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
			var encoder = new LZ4FrameWriter<ByteSpanAdapter, int>(
				new ByteSpanAdapter(UnsafeByteSpan.Create(compressedPtr, compressedBytes.Length)),
				0,
				d => d.CreateEncoder(),
				DefaultSettings);

			WriteAllBytes(originalBytes, encoder);
			var compressedLength = encoder.StreamState;

			var decoder = new LZ4FrameReader<ByteSpanAdapter, int>(
				new ByteSpanAdapter(UnsafeByteSpan.Create(compressedPtr, compressedLength)),
				0,
				d => d.CreateDecoder());

			var decompressedBytes = ReadAllBytes(decoder);
			Tools.SameBytes(originalBytes.AsSpan(), decompressedBytes.Span);
		}
	}
	
	[Theory]
	[InlineData(".corpus/webster")]
	[InlineData(".corpus/mozilla")]
	[InlineData(".corpus/dickens")]
	public void RoundtripWithByteSequence(string filename)
	{
		filename = Tools.FindFile(filename);
		var originalBytes = File.ReadAllBytes(filename);

		var compressedBytes = new byte[LZ4Codec.MaximumOutputSize(originalBytes.Length)];

		var encoder = new LZ4FrameWriter<ByteMemoryWriteAdapter, int>(
			new ByteMemoryWriteAdapter(new Memory<byte>(compressedBytes)),
			0,
			d => d.CreateEncoder(),
			DefaultSettings);

		WriteAllBytes(originalBytes, encoder);
		var compressedLength = encoder.StreamState;

		var decoder = new LZ4FrameReader<ByteSequenceAdapter, ReadOnlySequence<byte>>(
			new ByteSequenceAdapter(),
			ByteSegment.BuildSequence(compressedBytes.AsMemory(0, compressedLength), () => 1337),
			d => d.CreateDecoder());

		var decompressedBytes = ReadAllBytes(decoder);

		Tools.SameBytes(originalBytes.AsSpan(), decompressedBytes.Span);
	}

	private static void WriteAllBytes(ReadOnlyMemory<byte> buffer, ILZ4FrameWriter writer)
	{
		while (true)
		{
			var chunk = Math.Min(buffer.Length, 4096);
			if (chunk <= 0) break;

			writer.WriteManyBytes(buffer.Span.Slice(0, chunk));
			buffer = buffer.Slice(chunk);
		}

		writer.CloseFrame();
	}

	private static ReadOnlyMemory<byte> ReadAllBytes(ILZ4FrameReader reader)
	{
		const int chunk = 4096;
		var writer = BufferWriter.New();
		while (true)
		{
			var bytes = reader.ReadManyBytes(writer.GetSpan(chunk));
			if (bytes <= 0) break;

			writer.Advance(bytes);
		}

		return writer.WrittenMemory;
	}
}
