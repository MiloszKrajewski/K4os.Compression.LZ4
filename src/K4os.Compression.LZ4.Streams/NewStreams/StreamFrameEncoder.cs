using System;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using K4os.Compression.LZ4.Encoders;

namespace K4os.Compression.LZ4.Streams.NewStreams;

public class StreamFrameEncoder: FrameEncoder<StreamAdapter>
{
	public StreamFrameEncoder(
		Stream inner, 
		Func<ILZ4Descriptor, ILZ4Encoder> encoderFactory,
		ILZ4Descriptor descriptor): 
		base(new StreamAdapter(inner), encoderFactory, descriptor) { }

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public new long GetBytesWritten() => base.GetBytesWritten();
	
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public new void WriteOneByte(byte value) => 
		base.WriteOneByte(value);

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public new Task WriteOneByteAsync(byte value, CancellationToken token = default) =>
		base.WriteOneByteAsync(value, token);

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public new void WriteManyBytes(ReadOnlySpan<byte> buffer) =>
		base.WriteManyBytes(buffer);

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public new Task WriteManyBytesAsync(
		ReadOnlyMemory<byte> buffer, CancellationToken token = default) =>
		base.WriteManyBytesAsync(buffer, token);

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public new void CloseFrame() => 
		base.CloseFrame();

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public new Task CloseFrameAsync(CancellationToken token = default) =>
		base.CloseFrameAsync(token);
}
