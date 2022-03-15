using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using K4os.Compression.LZ4.Encoders;

namespace K4os.Compression.LZ4.Streams.NewStreams;

public class LZ4EncoderStream: LZ4StreamEssentials
{
	private readonly StreamFrameEncoder _encoder;

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
		_encoder = new StreamFrameEncoder(inner, encoderFactory, descriptor);
	}

	protected override void Dispose(bool disposing)
	{
		_encoder.Dispose();
		base.Dispose(disposing);
	}

	public override ValueTask DisposeAsync()
	{
		_encoder.Dispose();
		return base.DisposeAsync();
	}

	/// <inheritdoc />
	public override void WriteByte(byte value) =>
		_encoder.WriteOneByte(value);

	/// <inheritdoc />
	public override void Write(byte[] buffer, int offset, int count) =>
		_encoder.WriteManyBytes(buffer.AsSpan(offset, count));

	/// <inheritdoc />
	public override Task WriteAsync(
		byte[] buffer, int offset, int count, CancellationToken token) =>
		_encoder.WriteManyBytesAsync(buffer.AsMemory(offset, count), token);

	/// <inheritdoc />
	public override void Write(ReadOnlySpan<byte> buffer) =>
		_encoder.WriteManyBytes(buffer);

	/// <inheritdoc />
	public override ValueTask WriteAsync(
		ReadOnlyMemory<byte> buffer, CancellationToken token = default) =>
		new(_encoder.WriteManyBytesAsync(buffer, token));

	/// <inheritdoc />
	public override bool CanRead => false;

	/// <summary>Length of the stream and number of bytes written so far.</summary>
	public override long Length => _encoder.GetBytesWritten();

	/// <summary>Read-only position in the stream. Trying to set it will throw
	/// <see cref="InvalidOperationException"/>.</summary>
	public override long Position => _encoder.GetBytesWritten();
}
