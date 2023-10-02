using K4os.Compression.LZ4.Encoders;
using K4os.Compression.LZ4.Streams.Frames;
using K4os.Compression.LZ4.Streams.Internal;

namespace K4os.Compression.LZ4.Streams;

/// <summary>
/// LZ4 frame encoder stream.
/// </summary>
public class LZ4EncoderStream: LZ4StreamOnStreamEssentials
{
	private readonly StreamLZ4FrameWriter _writer;

	/// <summary>Creates new instance of <see cref="LZ4EncoderStream"/>.</summary>
	/// <param name="inner">Inner stream.</param>
	/// <param name="descriptor">LZ4 Descriptor.</param>
	/// <param name="encoderFactory">Function which will take descriptor and return
	/// appropriate encoder.</param>
	/// <param name="leaveOpen">Indicates if <paramref name="inner"/> stream should be left
	/// open after disposing.</param>
	public LZ4EncoderStream(
		Stream inner,
		ILZ4Descriptor descriptor,
		Func<ILZ4Descriptor, ILZ4Encoder> encoderFactory,
		bool leaveOpen = false):
		base(inner, leaveOpen)
	{
		_writer = new StreamLZ4FrameWriter(inner, true, encoderFactory, descriptor);
	}

	/// <inheritdoc />
	protected override void Dispose(bool disposing)
	{
		if (disposing) _writer.Dispose();
		base.Dispose(disposing);
	}
	
	/// <inheritdoc />
	public override void WriteByte(byte value) =>
		_writer.WriteOneByte(value);

	/// <inheritdoc />
	public override void Write(byte[] buffer, int offset, int count) =>
		_writer.WriteManyBytes(buffer.AsSpan(offset, count));

	/// <inheritdoc />
	public override Task WriteAsync(
		byte[] buffer, int offset, int count, CancellationToken token) =>
		_writer.WriteManyBytesAsync(token, buffer.AsMemory(offset, count));
	
	#if NETSTANDARD2_1_OR_GREATER || NET5_0_OR_GREATER

	/// <inheritdoc />
	public override async ValueTask DisposeAsync()
	{
		await _writer.DisposeAsync().Weave();
		await base.DisposeAsync().Weave();
	}

	/// <inheritdoc />
	public override void Write(ReadOnlySpan<byte> buffer) =>
		_writer.WriteManyBytes(buffer);

	/// <inheritdoc />
	public override ValueTask WriteAsync(
		ReadOnlyMemory<byte> buffer, CancellationToken token = default) =>
		new(_writer.WriteManyBytesAsync(token, buffer));

	#endif

	/// <inheritdoc />
	public override bool CanRead => false;

	/// <summary>Length of the stream and number of bytes written so far.</summary>
	public override long Length => _writer.GetBytesWritten();

	/// <summary>Read-only position in the stream. Trying to set it will throw
	/// <see cref="InvalidOperationException"/>.</summary>
	public override long Position => _writer.GetBytesWritten();
}
