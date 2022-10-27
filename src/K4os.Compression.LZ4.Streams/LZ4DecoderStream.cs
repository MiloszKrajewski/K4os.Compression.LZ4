using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using K4os.Compression.LZ4.Encoders;
using K4os.Compression.LZ4.Streams.Frames;
using K4os.Compression.LZ4.Streams.Internal;

namespace K4os.Compression.LZ4.Streams;

public class LZ4DecoderStream: LZ4StreamOnStreamEssentials
{
	private readonly StreamLZ4FrameReader _reader;
	private readonly bool _interactive;
	
	public LZ4DecoderStream(
		Stream inner,
		Func<ILZ4Descriptor, ILZ4Decoder> decoderFactory,
		bool leaveOpen = false,
		bool interactive = false):
		base(inner, leaveOpen)
	{
		_reader = new StreamLZ4FrameReader(inner, true, decoderFactory);
		_interactive = interactive;
	}

	/// <inheritdoc />
	public override int ReadByte() => 
		_reader.ReadOneByte();

	/// <inheritdoc />
	public override int Read(byte[] buffer, int offset, int count) =>
		_reader.ReadManyBytes(buffer.AsSpan(offset, count), _interactive);

	/// <inheritdoc />
	public override Task<int> ReadAsync(
		byte[] buffer, int offset, int count, CancellationToken token) =>
		_reader.ReadManyBytesAsync(token, buffer.AsMemory(offset, count), _interactive);

	/// <inheritdoc />
	public override bool CanWrite => false;

	/// <summary>
	/// Length of stream. Please note, this will only work if original LZ4 stream has
	/// <c>ContentLength</c> field set in descriptor. Otherwise returned value will be <c>-1</c>.
	/// It will also require synchronous stream access, so it wont work if AllowSynchronousIO
	/// is <c>false</c>.
	/// </summary>
	public override long Length => _reader.GetFrameLength() ?? -1;

	/// <summary>
	/// Position within the stream. Position can be read, but cannot be set as LZ4 stream does
	/// not have <c>Seek</c> capability.
	/// </summary>
	public override long Position => _reader.GetBytesRead();
	
	/// <inheritdoc />
	protected override void Dispose(bool disposing)
	{
		if (disposing) _reader.Dispose();
		base.Dispose(disposing);
	}
	
	#if NETSTANDARD2_1_OR_GREATER || NET5_0_OR_GREATER
	
	/// <inheritdoc />
	public override int Read(Span<byte> buffer) =>
		_reader.ReadManyBytes(buffer, _interactive);

	/// <inheritdoc />
	public override ValueTask<int> ReadAsync(
		Memory<byte> buffer, CancellationToken token = default) =>
		new(_reader.ReadManyBytesAsync(token, buffer, _interactive));

	/// <inheritdoc />
	public override async ValueTask DisposeAsync()
	{
		await _reader.DisposeAsync().Weave();
		await base.DisposeAsync().Weave();
	}
	
	#endif
}