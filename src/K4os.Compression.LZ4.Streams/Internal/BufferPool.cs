using System.Buffers;
using System.Runtime.CompilerServices;

namespace K4os.Compression.LZ4.Streams.Internal;

internal static class BufferPool
{
	private const int MinPoolBufferSize = 512;

	private static readonly ArrayPool<byte> ArrayPool = ArrayPool<byte>.Shared;

	/// <summary>Allocate temporary buffer to store decompressed data.</summary>
	/// <param name="size">Minimum size of the buffer.</param>
	/// <returns>Allocated buffer.</returns>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static byte[] Alloc(int size) =>
		size < MinPoolBufferSize ? new byte[size] : ArrayPool.Rent(size);

	/// <summary>Releases allocated buffer. <see cref="Alloc"/></summary>
	/// <param name="buffer">Previously allocated buffer.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static void Free(byte[] buffer)
	{
		if (buffer.Length >= MinPoolBufferSize)
			ArrayPool.Return(buffer);
	}
}
