using System;
using System.Buffers;
using System.IO.Pipelines;
using K4os.Compression.LZ4.Internal;
using K4os.Compression.LZ4.Streams.Abstractions;
using K4os.Compression.LZ4.Streams.Adapters;
using K4os.Compression.LZ4.Streams.Frames;
using TestHelpers;
using Xunit;

namespace K4os.Compression.LZ4.Streams.Tests;

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
			() => new LZ4FrameReader<ByteMemoryReadAdapter, int>(
				new ByteMemoryReadAdapter(buffer.WrittenMemory),
				0,
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
	
	[Theory]
	[InlineData(Mem.K64)]
	[InlineData(Mem.K128)]
	[InlineData(Mem.K256)]
	[InlineData(Mem.M1)]
	public async Task PipeReaderAndPipeWriter_Lorem(int size)
	{
		var bytes = new byte[size];
		Lorem.Fill(bytes, 0, bytes.Length);
		
		var random = new Random(0);
		var descriptor = new LZ4Descriptor(null, false, true, false, null, Mem.K64);

		var pipe = new Pipe();

		await PumpDataInParallel(
			random, bytes,
			() => new PipeLZ4FrameWriter(pipe.Writer, true, d => d.CreateEncoder(), descriptor),
			() => new PipeLZ4FrameReader(pipe.Reader, true, d => d.CreateDecoder()));
	}
	
	[Theory]
	[InlineData(Mem.K32)]
	[InlineData(Mem.K64)]
	[InlineData(Mem.K128)]
	[InlineData(Mem.K256)]
	[InlineData(Mem.M1)]
	public async Task PipeReaderAndPipeWriter_Random(int size)
	{
		var bytes = new byte[size];
		new Random(0).NextBytes(bytes);
		
		var random = new Random(0);
		var descriptor = new LZ4Descriptor(null, false, true, false, null, Mem.K64);

		var pipe = new Pipe();

		await PumpDataInParallel(
			random, bytes,
			() => new PipeLZ4FrameWriter(pipe.Writer, true, d => d.CreateEncoder(), descriptor),
			() => new PipeLZ4FrameReader(pipe.Reader, true, d => d.CreateDecoder()));
	}
	
	[Theory]
	[InlineData(".corpus/mozilla")]
	[InlineData(".corpus/dickens")]
	[InlineData(".corpus/reymont")]
	[InlineData(".corpus/x-ray")]
	public async Task PipeReaderAndPipeWriter(string filename)
	{
		var bytes = LoadFile(filename);
		var random = new Random(0);
		var descriptor = new LZ4Descriptor(null, false, true, false, null, Mem.K64);

		var pipe = new Pipe();

		await PumpDataInParallel(
			random, bytes,
			() => new PipeLZ4FrameWriter(pipe.Writer, true, d => d.CreateEncoder(), descriptor),
			() => new PipeLZ4FrameReader(pipe.Reader, true, d => d.CreateDecoder()));
	}

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
	
	private static async Task PumpDataInParallel(
		Random random,
		byte[] sourceBuffer,
		Func<ILZ4FrameWriter> encoderFactory,
		Func<ILZ4FrameReader> decoderFactory)
	{
		var targetBuffer = new byte[sourceBuffer.Length];
		
		var encoder = encoderFactory();
		var decoder = decoderFactory();
		var source = Tools.Adler32(sourceBuffer);
		
		var writerTask = JitterWrite(encoder, random, sourceBuffer);
		var readerTask = JitterRead(decoder, random, targetBuffer);
		
		await Task.WhenAll(readerTask, writerTask);
		
		var read = await readerTask;
		Assert.Equal(targetBuffer.Length, read);
		var target = Tools.Adler32(targetBuffer);
		Assert.Equal(target, source);
	}

	private static async Task<int> JitterRead(
		ILZ4FrameReader reader, Random random, byte[] buffer)
	{
		var position = 0;
		
		var chunkBuffer = new byte[0x1000];

		while (true)
		{
			var chunk = random.Next(1, chunkBuffer.Length);
			var read = await reader.ReadManyBytesAsync(chunkBuffer.AsMemory(0, chunk), true);
			if (read == 0) break;
			
			chunkBuffer.AsMemory(0, read).CopyTo(buffer.AsMemory(position, read));

			position += read;
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
