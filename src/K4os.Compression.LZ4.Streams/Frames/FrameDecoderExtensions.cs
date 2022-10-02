using System;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using K4os.Compression.LZ4.Streams.Abstractions;

namespace K4os.Compression.LZ4.Streams.Frames;

public static class FrameDecoderExtensions
{
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Task<int> ReadManyBytesAsync(
		this IFrameDecoder decoder, Memory<byte> buffer, bool interactive = false) =>
		decoder.ReadManyBytesAsync(CancellationToken.None, buffer, interactive);
}
