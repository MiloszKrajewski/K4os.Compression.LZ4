using System;
using System.Buffers;
using System.IO;
using System.Threading.Tasks;
using K4os.Compression.LZ4.Encoders;
using K4os.Compression.LZ4.Streams.Adapters;

#if NET5_0_OR_GREATER
using System.IO.Pipelines;
#endif

namespace K4os.Compression.LZ4.Streams.Frames;

public class ByteSpanLZ4FrameReader: LZ4FrameReader<ByteSpanAdapter, UnsafeByteSpan>
{
	public ByteSpanLZ4FrameReader(
		UnsafeByteSpan span, Func<ILZ4Descriptor, ILZ4Decoder> decoderFactory):
		base(new ByteSpanAdapter(), span, decoderFactory) { }
}

public class ByteMemoryLZ4FrameReader: LZ4FrameReader<ByteMemoryAdapter, ReadOnlyMemory<byte>>
{
	public ByteMemoryLZ4FrameReader(
		ReadOnlyMemory<byte> memory, Func<ILZ4Descriptor, ILZ4Decoder> decoderFactory):
		base(new ByteMemoryAdapter(), memory, decoderFactory) { }
}

public class ByteSequenceLZ4FrameReader: LZ4FrameReader<ByteSequenceAdapter, ReadOnlySequence<byte>>
{
	public ByteSequenceLZ4FrameReader(
		ReadOnlySequence<byte> sequence, Func<ILZ4Descriptor, ILZ4Decoder> decoderFactory):
		base(new ByteSequenceAdapter(), sequence, decoderFactory) { }
}

public class StreamLZ4FrameReader: LZ4FrameReader<StreamAdapter, EmptyState>
{
	private readonly Stream _stream;
	private readonly bool _leaveOpen;

	public StreamLZ4FrameReader(
		Stream stream, bool leaveOpen, Func<ILZ4Descriptor, ILZ4Decoder> decoderFactory):
		base(new StreamAdapter(stream), default, decoderFactory)
	{
		_stream = stream;
		_leaveOpen = leaveOpen;
	}

	protected override void Dispose(bool disposing)
	{
		CloseFrame();
		if (disposing && !_leaveOpen) _stream.Dispose();
		base.Dispose(disposing);
	}
	
	#if NETSTANDARD2_1_OR_GREATER || NET5_0_OR_GREATER

	public override async ValueTask DisposeAsync()
	{
		CloseFrame();
		if (!_leaveOpen) await _stream.DisposeAsync();
		await base.DisposeAsync();
	}
	
	#endif
}

#if NET5_0_OR_GREATER

public class PipeLZ4FrameReader: LZ4FrameReader<PipeReaderAdapter, ReadOnlySequence<byte>>
{
	private readonly PipeReader _pipe;
	private readonly bool _leaveOpen;

	public PipeLZ4FrameReader(
		PipeReader pipe, bool leaveOpen, Func<ILZ4Descriptor, ILZ4Decoder> decoderFactory):
		base(new PipeReaderAdapter(pipe), ReadOnlySequence<byte>.Empty, decoderFactory)
	{
		_pipe = pipe;
		_leaveOpen = leaveOpen;
	}

	protected override void Dispose(bool disposing)
	{
		CloseFrame();
		if (disposing && !_leaveOpen) _pipe.Complete();
		base.Dispose(disposing);
	}
	
	public override async ValueTask DisposeAsync()
	{
		CloseFrame();
		if (!_leaveOpen) await _pipe.CompleteAsync();
		await base.DisposeAsync();
	}
}

#endif
