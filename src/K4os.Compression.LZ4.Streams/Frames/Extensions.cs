using System;
using System.Buffers;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using K4os.Compression.LZ4.Streams.Abstractions;

namespace K4os.Compression.LZ4.Streams.Frames;

public static class Extensions
{
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Task<int> ReadManyBytesAsync(
		this IFrameDecoder decoder, Memory<byte> buffer, bool interactive = false) =>
		decoder.ReadManyBytesAsync(CancellationToken.None, buffer, interactive);

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static void CopyTo<TBufferWriter>(
		this IFrameDecoder decoder, TBufferWriter buffer, int blockSize = 0)
		where TBufferWriter: IBufferWriter<byte>
	{
		blockSize = Math.Max(blockSize, 4096);
		while (true)
		{
			var span = buffer.GetSpan(blockSize);
			var bytes = decoder.ReadManyBytes(span, true);
			if (bytes == 0) return;

			buffer.Advance(bytes);
		}
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static async Task CopyToAsync<TBufferWriter>(
		this IFrameDecoder decoder, TBufferWriter buffer, int blockSize = 0)
		where TBufferWriter: IBufferWriter<byte>
	{
		blockSize = Math.Max(blockSize, 4096);
		while (true)
		{
			var span = buffer.GetMemory(blockSize);
			var bytes = await decoder.ReadManyBytesAsync(span, true);
			if (bytes == 0) return;

			buffer.Advance(bytes);
		}
	}

	public static FrameDecoderAsStream AsStream(
		this IFrameDecoder decoder, bool leaveOpen = false, bool interactive = false) =>
		new(decoder, leaveOpen, interactive);

	public static FrameEncoderAsStream AsStream(
		this IFrameEncoder encoder, bool leaveOpen = false) => 
		new(encoder, leaveOpen);
}
