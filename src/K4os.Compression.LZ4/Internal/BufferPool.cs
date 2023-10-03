#nullable enable

using System;
using System.Buffers;
using System.Runtime.CompilerServices;

namespace K4os.Compression.LZ4.Internal;

/// <summary>
/// Naive wrapper around ArrayPool. Makes calls if something should be pooled. 
/// </summary>
public static class BufferPool
{
	/// <summary>Minimum size of the buffer that can be pooled.</summary>
	public const int MinPooledSize = 512;

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static bool ShouldBePooled(int length) =>
		length >= MinPooledSize;
	
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static byte[] Rent(int size, bool zero)
	{
		var array = ArrayPool<byte>.Shared.Rent(size);
		if (zero) array.AsSpan(0, size).Clear();
		return array;
	}

	/// <summary>Allocate temporary buffer to store decompressed data.</summary>
	/// <param name="size">Minimum size of the buffer.</param>
	/// <param name="zero">Clear all data.</param>
	/// <returns>Allocated buffer.</returns>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static byte[] Alloc(int size, bool zero = false) =>
		ShouldBePooled(size) ? Rent(size, zero) : new byte[size];

	/// <summary>
	/// Determines if buffer was pooled or not.
	/// The logic is quite simple: if buffer is smaller than 512 bytes are pooled.
	/// </summary>
	/// <param name="buffer">Buffer.</param>
	/// <returns><c>true</c> if buffer was pooled; <c>false</c> otherwise</returns>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static bool IsPooled(byte[] buffer) =>
		ShouldBePooled(buffer.Length);

	/// <summary>Releases allocated buffer. <see cref="Alloc"/></summary>
	/// <param name="buffer">Previously allocated buffer.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static void Free(byte[]? buffer)
	{
		if (buffer is not null && IsPooled(buffer))
			ArrayPool<byte>.Shared.Return(buffer);
	}
}
