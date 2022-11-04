using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using K4os.Compression.LZ4.Streams.Abstractions;
using K4os.Compression.LZ4.Streams.Internal;

namespace K4os.Compression.LZ4.Streams.Frames;

/// <summary>
/// Adapter to make <see cref="ILZ4FrameWriter"/> look like <see cref="Stream"/>.
/// </summary>
public class LZ4FrameWriterAsStream: LZ4StreamEssentials<ILZ4FrameWriter>
{
	/// <summary>Creates new instance of <see cref="LZ4EncoderStream"/>.</summary>
	/// <param name="writer">Underlying frame encoder.</param>
	/// <param name="leaveOpen">Indicates <paramref name="writer"/> should be left
	/// open after disposing.</param>
	public LZ4FrameWriterAsStream(ILZ4FrameWriter writer, bool leaveOpen = false):
		base(writer, leaveOpen) { }

	/// <inheritdoc />
	public override bool CanWrite => true;

	/// <inheritdoc />
	public override void WriteByte(byte value) =>
		InnerResource.WriteOneByte(value);

	/// <inheritdoc />
	public override void Write(byte[] buffer, int offset, int count) =>
		InnerResource.WriteManyBytes(buffer.AsSpan(offset, count));

	/// <inheritdoc />
	public override Task WriteAsync(
		byte[] buffer, int offset, int count, CancellationToken token) =>
		InnerResource.WriteManyBytesAsync(token, buffer.AsMemory(offset, count));

	#if NETSTANDARD2_1_OR_GREATER || NET5_0_OR_GREATER

	/// <inheritdoc />
	public override void Write(ReadOnlySpan<byte> buffer) =>
		InnerResource.WriteManyBytes(buffer);

	/// <inheritdoc />
	public override ValueTask WriteAsync(
		ReadOnlyMemory<byte> buffer, CancellationToken token = default) =>
		new(InnerResource.WriteManyBytesAsync(token, buffer));

	#endif

	/// <summary>Length of the stream and number of bytes written so far.</summary>
	public override long Length => InnerResource.GetBytesWritten();

	/// <summary>Read-only position in the stream. Trying to set it will throw
	/// <see cref="InvalidOperationException"/>.</summary>
	public override long Position => InnerResource.GetBytesWritten();
}
