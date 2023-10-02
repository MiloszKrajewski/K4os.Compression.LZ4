using System.Buffers;
using System.IO.Pipelines;
using K4os.Compression.LZ4.Encoders;
using K4os.Compression.LZ4.Streams.Abstractions;
using K4os.Compression.LZ4.Streams.Adapters;
using K4os.Compression.LZ4.Streams.Internal;

namespace K4os.Compression.LZ4.Streams.Frames;

/// <summary>
/// <see cref="ILZ4FrameWriter"/> implementation for <see cref="IBufferWriter{T}"/>
/// </summary>
/// <typeparam name="TBufferWriter">Type of buffer writer.</typeparam>
public class ByteBufferLZ4FrameWriter<TBufferWriter>:
	LZ4FrameWriter<ByteBufferAdapter<TBufferWriter>, TBufferWriter>
	where TBufferWriter: IBufferWriter<byte>
{
	/// <summary>
	/// Creates new instance of <see cref="ByteBufferLZ4FrameWriter{TBufferWriter}"/>.
	/// </summary>
	/// <param name="stream">Buffer writer to write to.</param>
	/// <param name="encoderFactory">Encoder factory.</param>
	/// <param name="descriptor">Frame descriptor.</param>
	public ByteBufferLZ4FrameWriter(
		TBufferWriter stream,
		Func<ILZ4Descriptor, ILZ4Encoder> encoderFactory,
		ILZ4Descriptor descriptor):
		base(new ByteBufferAdapter<TBufferWriter>(), stream, encoderFactory, descriptor) { }
	
	/// <summary>Current state of buffer writer.</summary>
	public TBufferWriter BufferWriter => StreamState;
}

/// <summary>
/// <see cref="ILZ4FrameWriter"/> implementation for <see cref="IBufferWriter{T}"/>
/// </summary>
public class ByteBufferLZ4FrameWriter: ByteBufferLZ4FrameWriter<IBufferWriter<byte>>
{
	/// <summary>
	/// Creates new instance of <see cref="ByteBufferLZ4FrameWriter"/>.
	/// </summary>
	/// <param name="stream">Buffer writer to write to.</param>
	/// <param name="encoderFactory">Encoder factory.</param>
	/// <param name="descriptor">Frame descriptor.</param>
	public ByteBufferLZ4FrameWriter(
		IBufferWriter<byte> stream,
		Func<ILZ4Descriptor, ILZ4Encoder> encoderFactory,
		ILZ4Descriptor descriptor):
		base(stream, encoderFactory, descriptor) { }
}

/// <summary>
/// <see cref="ILZ4FrameWriter"/> implementation for <see cref="Memory{T}"/>
/// </summary>
public class ByteMemoryLZ4FrameWriter: LZ4FrameWriter<ByteMemoryWriteAdapter, int>
{
	/// <summary>
	/// Creates new instance of <see cref="ByteMemoryLZ4FrameWriter"/>.
	/// </summary>
	/// <param name="memory">Memory block where data will be written.</param>
	/// <param name="encoderFactory">Encoder factory.</param>
	/// <param name="descriptor">Frame descriptor.</param>
	public ByteMemoryLZ4FrameWriter(
		Memory<byte> memory,
		Func<ILZ4Descriptor, ILZ4Encoder> encoderFactory,
		ILZ4Descriptor descriptor): base(
		new ByteMemoryWriteAdapter(memory), 0, encoderFactory, descriptor) { }

	/// <summary>Number of bytes written to the memory.</summary>
	public int CompressedLength => StreamState;
}

/// <summary>
/// <see cref="ILZ4FrameWriter"/> implementation for <see cref="UnsafeByteSpan"/>.
/// <see cref="UnsafeByteSpan"/> is a wrapper around <see cref="Span{T}"/> that
/// can be stored in a field. Please note: it makes it unsafe and address needs to be pinned,
/// one way or another.
/// </summary>
public class ByteSpanLZ4FrameWriter: LZ4FrameWriter<ByteSpanAdapter, int>
{
	/// <summary>
	/// Creates new instance of <see cref="ByteSpanLZ4FrameWriter"/>.
	/// </summary>
	/// <param name="span">Span to write to.</param>
	/// <param name="encoderFactory">Encoder factory.</param>
	/// <param name="descriptor">Frame descriptor.</param>
	public ByteSpanLZ4FrameWriter(
		UnsafeByteSpan span,
		Func<ILZ4Descriptor, ILZ4Encoder> encoderFactory,
		ILZ4Descriptor descriptor):
		base(new ByteSpanAdapter(span), 0, encoderFactory, descriptor) { }
	
	/// <summary>Number of bytes written to the memory.</summary>
	public int CompressedLength => StreamState;
}

/// <summary>
/// <see cref="ILZ4FrameWriter"/> implementation for <see cref="Stream"/>.
/// </summary>
public class StreamLZ4FrameWriter: LZ4FrameWriter<StreamAdapter, EmptyState>
{
	private readonly Stream _stream;
	private readonly bool _leaveOpen;

	/// <summary>
	/// Creates new instance of <see cref="StreamLZ4FrameWriter"/>.
	/// </summary>
	/// <param name="stream">Stream to write to.</param>
	/// <param name="leaveOpen">Leave stream open after disposing this writer.</param>
	/// <param name="encoderFactory">Encoder factory.</param>
	/// <param name="descriptor">Frame descriptor.</param>
	public StreamLZ4FrameWriter(
		Stream stream,
		bool leaveOpen,
		Func<ILZ4Descriptor, ILZ4Encoder> encoderFactory,
		ILZ4Descriptor descriptor):
		base(new StreamAdapter(stream), default, encoderFactory, descriptor)
	{
		_stream = stream;
		_leaveOpen = leaveOpen;
	}

	/// <inheritdoc />
	protected override void ReleaseResources()
	{
		if (!_leaveOpen) _stream.Dispose();
		base.ReleaseResources();
	}
	
	/// <inheritdoc />
	protected override async Task ReleaseResourcesAsync()
	{
		#if NETSTANDARD2_1_OR_GREATER || NET5_0_OR_GREATER
		if (!_leaveOpen) await _stream.DisposeAsync().Weave();
		#else
		if (!_leaveOpen) _stream.Dispose();
		#endif
		await base.ReleaseResourcesAsync().Weave();
	}
}

/// <summary>
/// <see cref="ILZ4FrameWriter"/> implementation for <see cref="PipeWriter"/>.
/// </summary>
public class PipeLZ4FrameWriter: LZ4FrameWriter<PipeWriterAdapter, EmptyState>
{
	private readonly PipeWriter _pipe;
	private readonly bool _leaveOpen;
	
	/// <summary>
	/// Creates new instance of <see cref="PipeLZ4FrameWriter"/>.
	/// </summary>
	/// <param name="pipe">Pipe writer to write to.</param>
	/// <param name="leaveOpen">Leave pipe open after disposing this writer.</param>
	/// <param name="encoderFactory">Encoder factory.</param>
	/// <param name="descriptor">Frame descriptor.</param>
	public PipeLZ4FrameWriter(
		PipeWriter pipe,
		bool leaveOpen,
		Func<ILZ4Descriptor, ILZ4Encoder> encoderFactory,
		ILZ4Descriptor descriptor):
		base(new PipeWriterAdapter(pipe), default, encoderFactory, descriptor)
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
