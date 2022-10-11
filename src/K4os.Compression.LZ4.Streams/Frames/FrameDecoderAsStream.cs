using System;
using System.Threading;
using System.Threading.Tasks;
using K4os.Compression.LZ4.Streams.Abstractions;
using K4os.Compression.LZ4.Streams.Internal;

namespace K4os.Compression.LZ4.Streams.Frames;

public class FrameDecoderAsStream: LZ4StreamEssentials<IFrameDecoder>
{
	private readonly bool _interactive;

	public FrameDecoderAsStream(IFrameDecoder decoder, bool leaveOpen, bool interactive):
		base(decoder, leaveOpen)
	{
		_interactive = interactive;
	}

	/// <inheritdoc />
	public override bool CanRead => true;

	/// <inheritdoc />
	public override int ReadByte() =>
		InnerResource.ReadOneByte();

	/// <inheritdoc />
	public override int Read(byte[] buffer, int offset, int count) =>
		InnerResource.ReadManyBytes(buffer.AsSpan(offset, count), _interactive);

	/// <inheritdoc />
	public override Task<int> ReadAsync(
		byte[] buffer, int offset, int count, CancellationToken token) =>
		InnerResource.ReadManyBytesAsync(token, buffer.AsMemory(offset, count), _interactive);

	/// <summary>
	/// Length of stream. Please note, this will only work if original LZ4 stream has
	/// <c>ContentLength</c> field set in descriptor. Otherwise returned value will be <c>-1</c>.
	/// It will also require synchronous stream access, so it wont work if AllowSynchronousIO
	/// is <c>false</c>.
	/// </summary>
	public override long Length =>
		InnerResource.GetFrameLength() ?? -1;

	/// <summary>
	/// Position within the stream. Position can be read, but cannot be set as LZ4 stream does
	/// not have <c>Seek</c> capability.
	/// </summary>
	public override long Position =>
		InnerResource.GetBytesRead();

	/// <inheritdoc />
	protected override void Dispose(bool disposing)
	{
		if (disposing) InnerResource.Dispose();
		base.Dispose(disposing);
	}

	#if NETSTANDARD2_1_OR_GREATER || NET5_0_OR_GREATER

	/// <inheritdoc />
	public override int Read(Span<byte> buffer) =>
		InnerResource.ReadManyBytes(buffer, _interactive);

	/// <inheritdoc />
	public override ValueTask<int> ReadAsync(
		Memory<byte> buffer, CancellationToken token = default) =>
		new(InnerResource.ReadManyBytesAsync(token, buffer, _interactive));

	/// <inheritdoc />
	public override async ValueTask DisposeAsync()
	{
		await InnerResource.DisposeAsync().Weave();
		await base.DisposeAsync().Weave();
	}

	#endif
}