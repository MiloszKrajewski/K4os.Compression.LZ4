using System;
using System.Buffers;
using System.IO;
using System.IO.Pipelines;
using System.Threading.Tasks;
using K4os.Compression.LZ4.Encoders;
using K4os.Compression.LZ4.Streams.Abstractions;
using K4os.Compression.LZ4.Streams.Adapters;
using K4os.Compression.LZ4.Streams.Internal;

namespace K4os.Compression.LZ4.Streams.Frames;

/// <summary>
/// <see cref="ILZ4FrameReader"/> implementation for <see cref="UnsafeByteSpan"/>.
/// </summary>
public class ByteSpanLZ4FrameReader: 
	LZ4FrameReader<ByteSpanAdapter, int>
{
	/// <summary>
	/// Creates new instance of <see cref="ByteSpanLZ4FrameReader"/>.
	/// </summary>
	/// <param name="span">Bytes span.</param>
	/// <param name="decoderFactory">LZ4 decoder factory.</param>
	public ByteSpanLZ4FrameReader(
		UnsafeByteSpan span, Func<ILZ4Descriptor, ILZ4Decoder> decoderFactory):
		base(new ByteSpanAdapter(span), 0, decoderFactory) { }
}

/// <summary>
/// <see cref="ILZ4FrameReader"/> implementation for <see cref="ReadOnlyMemory{T}"/>.
/// </summary>
public class ByteMemoryLZ4FrameReader: 
	LZ4FrameReader<ByteMemoryReadAdapter, int>
{
	/// <summary>
	/// Creates new instance of <see cref="ByteMemoryLZ4FrameReader"/>.
	/// </summary>
	/// <param name="memory">Memory buffer.</param>
	/// <param name="decoderFactory">LZ4 decoder factory.</param>
	public ByteMemoryLZ4FrameReader(
		ReadOnlyMemory<byte> memory, Func<ILZ4Descriptor, ILZ4Decoder> decoderFactory):
		base(new ByteMemoryReadAdapter(memory), 0, decoderFactory) { }
}

/// <summary>
/// <see cref="ILZ4FrameReader"/> implementation for <see cref="ReadOnlySequence{T}"/>.
/// </summary>
public class ByteSequenceLZ4FrameReader: 
	LZ4FrameReader<ByteSequenceAdapter, ReadOnlySequence<byte>>
{
	/// <summary>
	/// Creates new instance of <see cref="ByteSequenceLZ4FrameReader"/>.
	/// </summary>
	/// <param name="sequence">Byte sequence.</param>
	/// <param name="decoderFactory">LZ4 decoder factory.</param>
	public ByteSequenceLZ4FrameReader(
		ReadOnlySequence<byte> sequence, Func<ILZ4Descriptor, ILZ4Decoder> decoderFactory):
		base(new ByteSequenceAdapter(), sequence, decoderFactory) { }
}

/// <summary>
/// <see cref="ILZ4FrameReader"/> implementation for <see cref="Stream"/>.
/// </summary>
public class StreamLZ4FrameReader: LZ4FrameReader<StreamAdapter, EmptyState>
{
	private readonly Stream _stream;
	private readonly bool _leaveOpen;

	/// <summary>
	/// Creates new instance of <see cref="StreamLZ4FrameReader"/>.
	/// </summary>
	/// <param name="stream">Stream to read from.</param>
	/// <param name="leaveOpen">Leave stream open after reader is disposed.</param>
	/// <param name="decoderFactory">LZ4 decoder factory.</param>
	public StreamLZ4FrameReader(
		Stream stream, bool leaveOpen, Func<ILZ4Descriptor, ILZ4Decoder> decoderFactory):
		base(new StreamAdapter(stream), default, decoderFactory)
	{
		_stream = stream;
		_leaveOpen = leaveOpen;
	}

	/// <summary>
	/// Disposes the reader.
	/// </summary>
	/// <param name="disposing"><c>true</c> if user is disposing it; <c>false</c> if it has been triggered by garbage collector</param>
	protected override void Dispose(bool disposing)
	{
		CloseFrame();
		if (disposing && !_leaveOpen) _stream.Dispose();
		base.Dispose(disposing);
	}
	
	#if NETSTANDARD2_1_OR_GREATER || NET5_0_OR_GREATER

	/// <summary>
	/// Disposes the reader.
	/// </summary>
	public override async ValueTask DisposeAsync()
	{
		CloseFrame();
		if (!_leaveOpen) await _stream.DisposeAsync().Weave();
		await base.DisposeAsync().Weave();
	}
	
	#endif
}

/// <summary>
/// <see cref="ILZ4FrameReader"/> implementation for <see cref="PipeReader"/>.
/// </summary>
public class PipeLZ4FrameReader: LZ4FrameReader<PipeReaderAdapter, EmptyState>
{
	private readonly PipeReader _pipe;
	private readonly bool _leaveOpen;

	/// <summary>
	/// Creates new instance of <see cref="PipeLZ4FrameReader"/>.
	/// </summary>
	/// <param name="pipe">Pipe to be read.</param>
	/// <param name="leaveOpen">Leave pipe open after reader is disposed.</param>
	/// <param name="decoderFactory">LZ4 decoder factory.</param>
	public PipeLZ4FrameReader(
		PipeReader pipe, bool leaveOpen, Func<ILZ4Descriptor, ILZ4Decoder> decoderFactory):
		base(new PipeReaderAdapter(pipe), default, decoderFactory)
	{
		_pipe = pipe;
		_leaveOpen = leaveOpen;
	}

	/// <inheritdoc />
	protected override void ReleaseResources()
	{
		if (!_leaveOpen) _pipe.Complete();
		base.ReleaseResources();
	}

	/// <inheritdoc />
	protected override async Task ReleaseResourcesAsync()
	{
		if (!_leaveOpen) await _pipe.CompleteAsync().Weave();
		await base.ReleaseResourcesAsync().Weave();
	}
}
