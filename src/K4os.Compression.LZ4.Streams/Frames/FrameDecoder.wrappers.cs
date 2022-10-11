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

public class ByteSpanFrameDecoder: FrameDecoder<UnsafeByteSpanAdapter, UnsafeByteSpan>
{
	public ByteSpanFrameDecoder(
		UnsafeByteSpan span, Func<ILZ4Descriptor, ILZ4Decoder> decoderFactory):
		base(new UnsafeByteSpanAdapter(), span, decoderFactory) { }
}

public class ByteMemoryFrameDecoder: FrameDecoder<ByteMemoryAdapter, ReadOnlyMemory<byte>>
{
	public ByteMemoryFrameDecoder(
		ReadOnlyMemory<byte> memory, Func<ILZ4Descriptor, ILZ4Decoder> decoderFactory):
		base(new ByteMemoryAdapter(), memory, decoderFactory) { }
}

public class ByteSequenceFrameDecoder: FrameDecoder<ByteSequenceAdapter, ReadOnlySequence<byte>>
{
	public ByteSequenceFrameDecoder(
		ReadOnlySequence<byte> sequence, Func<ILZ4Descriptor, ILZ4Decoder> decoderFactory):
		base(new ByteSequenceAdapter(), sequence, decoderFactory) { }
}

public class StreamFrameDecoder: FrameDecoder<StreamAdapter, EmptyState>
{
	private readonly Stream _stream;
	private readonly bool _leaveOpen;

	public StreamFrameDecoder(
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

public class PipeFrameDecoder:
	FrameDecoder<PipeReaderAdapter, ReadOnlySequence<byte>>
{
	private readonly PipeReader _pipe;
	private readonly bool _leaveOpen;

	public PipeFrameDecoder(
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
