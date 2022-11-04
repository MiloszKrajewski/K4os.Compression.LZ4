using System;
using System.Runtime.CompilerServices;

namespace K4os.Compression.LZ4.Streams.Adapters;

/// <summary>
/// Unsafe version of <see cref="Span{T}"/>. It is unsafe as it stores raw memory pointer
/// so memory it points to must be pinned. It allows reading and writing straight to
/// unmanaged memory but must be used carefully.
/// NOTE: If you don't understand what has been said above - don't use it. Misuse of this
/// struct may lead to unpredictable errors and memory corruption. 
/// </summary>
public readonly unsafe struct UnsafeByteSpan
{
	/// <summary>Pointer to the first byte of the span.</summary>
	public byte* Bytes { get; }

	/// <summary>Length of the span in bytes.</summary>
	public int Length { get; }

	/// <summary>
	/// Creates new instance of <see cref="UnsafeByteSpan"/> from given pointer and length.
	/// </summary>
	/// <param name="bytes">Pointer to the first byte of the span.</param>
	/// <param name="length">Length of the span in bytes.</param>
	public UnsafeByteSpan(void* bytes, int length)
	{
		Bytes = (byte*)bytes;
		Length = length;
	}

	/// <summary>
	/// Creates new instance of <see cref="UnsafeByteSpan"/> from raw pointer.
	/// </summary>
	/// <param name="bytes">Pointer block of bytes.</param>
	/// <param name="length">Length of the block.</param>
	/// <returns>New <see cref="UnsafeByteSpan"/>.</returns>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static UnsafeByteSpan Create(void* bytes, int length) =>
		new(bytes, length);

	/// <summary>
	/// Converted to <see cref="Span{T}"/>.
	/// </summary>
	public Span<byte> Span
	{
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => new(Bytes, Length);
	}
}
