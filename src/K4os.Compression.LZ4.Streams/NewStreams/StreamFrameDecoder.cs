using System;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using K4os.Compression.LZ4.Encoders;

namespace K4os.Compression.LZ4.Streams.NewStreams;

public class StreamFrameDecoder: FrameDecoder<StreamAdapter>
{
	public StreamFrameDecoder(
		Stream stream, Func<ILZ4Descriptor, ILZ4Decoder> decoderFactory):
		base(new StreamAdapter(stream), decoderFactory) { }

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public new long GetBytesRead() =>
		base.GetBytesRead();

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public new long GetFrameLength() =>
		base.GetFrameLength();

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public new Task<long> GetFrameLengthAsync(CancellationToken token = default) =>
		base.GetFrameLengthAsync(token);

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public new int ReadOneByte() =>
		base.ReadOneByte();

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public new Task<int> ReadOneByteAsync(CancellationToken token = default) =>
		base.ReadOneByteAsync(token);

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public new int ReadManyBytes(Span<byte> buffer, bool interactive = false) =>
		base.ReadManyBytes(buffer, interactive);

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public new Task<int> ReadManyBytesAsync(
		CancellationToken token, Memory<byte> buffer, bool interactive = false) =>
		base.ReadManyBytesAsync(token, buffer, interactive);

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public new void CloseFrame() =>
		base.CloseFrame();
}