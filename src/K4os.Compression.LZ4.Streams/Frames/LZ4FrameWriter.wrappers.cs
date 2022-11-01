using System;
using System.Buffers;
using System.IO;
using K4os.Compression.LZ4.Encoders;
using K4os.Compression.LZ4.Streams.Abstractions;
using K4os.Compression.LZ4.Streams.Adapters;

#if NET5_0_OR_GREATER
using System.IO.Pipelines;
using System.Threading.Tasks;
#endif

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

public class ByteMemoryLZ4FrameWriter: LZ4FrameWriter<ByteMemoryWriteAdapter, int>
{
	public ByteMemoryLZ4FrameWriter(
		Memory<byte> stream,
		Func<ILZ4Descriptor, ILZ4Encoder> encoderFactory,
		ILZ4Descriptor descriptor): base(
		new ByteMemoryWriteAdapter(stream), 0, encoderFactory, descriptor) { }
}

public class ByteSpanLZ4FrameWriter: LZ4FrameWriter<ByteSpanAdapter, int>
{
	public ByteSpanLZ4FrameWriter(
		UnsafeByteSpan stream,
		Func<ILZ4Descriptor, ILZ4Encoder> encoderFactory,
		ILZ4Descriptor descriptor):
		base(new ByteSpanAdapter(stream), 0, encoderFactory, descriptor) { }
}

public class StreamLZ4FrameWriter: LZ4FrameWriter<StreamAdapter, EmptyState>
{
	private readonly Stream _stream;
	private readonly bool _leaveOpen;

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

	protected override void ReleaseResources()
	{
		if (!_leaveOpen) _stream.Dispose();
		base.ReleaseResources();
	}

	#if NETSTANDARD2_1_OR_GREATER || NET5_0_OR_GREATER

	protected override async Task ReleaseResourcesAsync()
	{
		if (!_leaveOpen) await _stream.DisposeAsync();
		await base.ReleaseResourcesAsync();
	}
	
	#endif
}

#if NET5_0_OR_GREATER

public class PipeLZ4FrameWriter: LZ4FrameWriter<PipeWriterAdapter, EmptyState>
{
	private readonly PipeWriter _stream;
	private readonly bool _leaveOpen;
	
	public PipeLZ4FrameWriter(
		PipeWriter stream,
		bool leaveOpen,
		Func<ILZ4Descriptor, ILZ4Encoder> encoderFactory,
		ILZ4Descriptor descriptor):
		base(new PipeWriterAdapter(stream), default, encoderFactory, descriptor)
	{
		_stream = stream;
		_leaveOpen = leaveOpen;
	}
	
	protected override void Dispose(bool disposing)
	{
		CloseFrame();
		if (disposing && !_leaveOpen) _stream.Complete();
		base.Dispose(disposing);
	}
	
	public override async ValueTask DisposeAsync()
	{
		await CloseFrameAsync();
		if (!_leaveOpen) await _stream.CompleteAsync();
		await base.DisposeAsync();
	}
}

#endif
