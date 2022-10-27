using System;
using System.Buffers;
using System.IO;
using System.Threading.Tasks;
using K4os.Compression.LZ4.Internal;
using K4os.Compression.LZ4.Streams.Abstractions;
using K4os.Compression.LZ4.Streams.Adapters;
using K4os.Compression.LZ4.Streams.Frames;
using TestHelpers;
using Xunit;

#if NET5_0_OR_GREATER
using System.IO.Pipelines;
#endif

namespace K4os.Compression.LZ4.Streams.Test;

public class FrameRountripTests
{
	[Theory]
	[InlineData(".corpus/mozilla")]
	public async Task BufferWriterAndMemoryReaderWorkTogether(string filename)
	{
		var bytes = LoadFile(filename);
		var random = new Random(0);
		var descriptor = new LZ4Descriptor(null, false, true, false, null, Mem.K64);

		var buffer = new BufferWriter();

		await PumpData(
			random, bytes,
			() => new LZ4FrameWriter<ByteBufferAdapter<BufferWriter>, BufferWriter>(
				new ByteBufferAdapter<BufferWriter>(),
				buffer,
				d => d.CreateEncoder(),
				descriptor),
			() => new LZ4FrameReader<ByteMemoryAdapter, ReadOnlyMemory<byte>>(
				new ByteMemoryAdapter(),
				buffer.WrittenMemory,
				d => d.CreateDecoder()));
	}

	private static byte[] LoadFile(string filename) => 
		File.ReadAllBytes(Tools.FindFile(filename));

	[Theory]
	[InlineData(".corpus/mozilla")]
	public async Task BufferWriterAndByteSequenceWorkTogether(string filename)
	{
		var bytes = LoadFile(filename);
		var random = new Random(0);
		var descriptor = new LZ4Descriptor(null, false, true, false, null, Mem.K64);

		var buffer = new BufferWriter();

		await PumpData(
			random, bytes,
			() => new LZ4FrameWriter<ByteBufferAdapter<BufferWriter>, BufferWriter>(
				new ByteBufferAdapter<BufferWriter>(),
				buffer,
				d => d.CreateEncoder(),
				descriptor),
			() => new LZ4FrameReader<ByteSequenceAdapter, ReadOnlySequence<byte>>(
				new ByteSequenceAdapter(),
				new ReadOnlySequence<byte>(buffer.WrittenMemory),
				d => d.CreateDecoder()));
	}

	[Theory]
	[InlineData(".corpus/mozilla")]
	public async Task BufferWriterAndRandomizedByteSequenceWorkTogether(string filename)
	{
		var bytes = LoadFile(filename);
		var random = new Random(0);
		var descriptor = new LZ4Descriptor(null, false, true, false, null, Mem.K64);

		var buffer = new BufferWriter();

		await PumpData(
			random, bytes,
			() => new LZ4FrameWriter<ByteBufferAdapter<BufferWriter>, BufferWriter>(
				new ByteBufferAdapter<BufferWriter>(),
				buffer,
				d => d.CreateEncoder(),
				descriptor),
			() => new LZ4FrameReader<ByteSequenceAdapter, ReadOnlySequence<byte>>(
				new ByteSequenceAdapter(),
				ByteSegment.BuildSequence(buffer.WrittenMemory, () => random.Next(Mem.K64)),
				d => d.CreateDecoder()));
	}
	
	[Theory]
	[InlineData(".corpus/mozilla")]
	public async Task MemoryStreamAlsoWorks(string filename)
	{
		var bytes = LoadFile(filename);
		var random = new Random(0);
		var descriptor = new LZ4Descriptor(null, false, true, false, null, Mem.K64);

		using Stream stream = new MemoryStream();

		await PumpData(
			random, bytes,
			() => new LZ4FrameWriter<StreamAdapter, EmptyState>(
				new StreamAdapter(stream),
				default,
				d => d.CreateEncoder(),
				descriptor),
			() => new LZ4FrameReader<StreamAdapter, EmptyState>(
				new StreamAdapter(Tools.Rewind(stream)),
				default,
				d => d.CreateDecoder()));
	}
	
	#if NET5_0_OR_GREATER
	
	[Theory(Skip = "Not working yet")]
	[InlineData(".corpus/mozilla")]
	public async Task PipeReaderAndPipeWriter(string filename)
	{
		var bytes = LoadFile(filename);
		var random = new Random(0);
		var descriptor = new LZ4Descriptor(null, false, true, false, null, Mem.K64);

		var pipe = new Pipe();

		await PumpData(
			random, bytes,
			() => new LZ4FrameWriter<PipeWriterAdapter, EmptyState>(
				new PipeWriterAdapter(pipe.Writer),
				default,
				d => d.CreateEncoder(),
				descriptor),
			() => new LZ4FrameReader<PipeReaderAdapter, ReadOnlySequence<byte>>(
				new PipeReaderAdapter(pipe.Reader),
				ReadOnlySequence<byte>.Empty, 
				d => d.CreateDecoder()));
	}

	#endif

	private static async Task PumpData(
		Random random,
		byte[] buffer,
		Func<ILZ4FrameWriter> encoderFactory,
		Func<ILZ4FrameReader> decoderFactory)
	{
		var encoder = encoderFactory();

		var source = Tools.Adler32(buffer);
		await JitterWrite(encoder, random, buffer);
		
		random.NextBytes(buffer);
		var garbage = Tools.Adler32(buffer);
		Assert.NotEqual(source, garbage);

		var decoder = decoderFactory();

		var read = await JitterRead(decoder, random, buffer);
		Assert.Equal(buffer.Length, read);
		var target = Tools.Adler32(buffer);
		Assert.Equal(target, source);
	}

	private static async Task<int> JitterRead(
		ILZ4FrameReader reader, Random random, byte[] buffer)
	{
		var length = buffer.Length;
		var position = 0;

		while (true)
		{
			if (length <= 0) break;

			var chunk = random.Next(1, Math.Min(0x1000, length));
			var read = await reader.ReadManyBytesAsync(buffer.AsMemory(position, chunk), true);
			if (read == 0) break;

			position += read;
			length -= read;
		}

		return position;
	}

	private static async Task JitterWrite(
		ILZ4FrameWriter writer, Random random, ReadOnlyMemory<byte> buffer)
	{
		var length = buffer.Length;
		var position = 0;

		await writer.OpenFrameAsync();

		while (length > 0)
		{
			var chunk = random.Next(1, Math.Min(0x1000, length));
			await writer.WriteManyBytesAsync(buffer.Slice(position, chunk));
			length -= chunk;
			position += chunk;
		}

		await writer.CloseFrameAsync();
	}
}
