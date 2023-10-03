#nullable enable

using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace K4os.Compression.LZ4.Internal;

/// <summary>
/// Represents pinned memory.
/// It either points to unmanaged memory or block of memory from shared array pool.
/// When disposed, it handles it appropriately.
/// </summary>
public unsafe struct PinnedMemory
{
	/// <summary>
	/// Maximum size of the buffer that can be pooled from shared array pool.
	/// </summary>
	public static int MaxPooledSize { get; set; } = Mem.M1;

	private byte* _pointer;
	private GCHandle _handle;
	private int _size;

	/// <summary>Pointer to block of bytes.</summary>
	public readonly byte* Pointer
	{
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => _pointer;
	}

	/// <summary>Pointer to block of bytes as span.</summary>
	public Span<byte> Span
	{
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => new(Pointer, _size);
	}

	/// <summary>Pointer to block of bytes.</summary>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public readonly T* Reference<T>() where T: unmanaged => (T*)_pointer;

	/// <summary>
	/// Allocates pinned block of memory, depending on the size it tries to use shared array pool.
	/// </summary>
	/// <param name="size">Size in bytes.</param>
	/// <param name="zero">Indicates if block should be zeroed.</param>
	/// <returns>Allocated <see cref="PinnedMemory"/>.</returns>
	/// <exception cref="ArgumentOutOfRangeException"></exception>
	public static PinnedMemory Alloc(int size, bool zero = true)
	{
		Alloc(out var result, size, zero);
		return result;
	}

	/// <summary>
	/// Allocates pinned block of memory, depending on the size it tries to use shared array pool.
	/// </summary>
	/// <param name="memory">Pinned memory pointer.</param>
	/// <param name="size">Size in bytes.</param>
	/// <param name="zero">Indicates if block should be zeroed.</param>
	/// <returns>Allocated <see cref="PinnedMemory"/>.</returns>
	/// <exception cref="ArgumentOutOfRangeException"></exception>
	public static void Alloc(out PinnedMemory memory, int size, bool zero = true)
	{
		if (size <= 0)
			throw new ArgumentOutOfRangeException(nameof(size));

		if (size > MaxPooledSize)
		{
			AllocateNative(out memory, size, zero);
		}
		else
		{
			RentManagedFromPool(out memory, size, zero);
		}
	}

	/// <summary>
	/// Allocates pinned block of memory for one item from shared array pool.
	/// </summary>
	/// <param name="memory">PinnedMemory pointer.</param>
	/// <param name="zero">Indicates if block should be zeroed.</param>
	/// <typeparam name="T">Type of item.</typeparam>
	public static void Alloc<T>(out PinnedMemory memory, bool zero = true) where T: unmanaged
	{
		Alloc(out memory, sizeof(T), zero);
	}

	private static void AllocateNative(out PinnedMemory memory, int size, bool zero)
	{
		var bytes = zero ? Mem.AllocZero(size) : Mem.Alloc(size);
		GC.AddMemoryPressure(size);

		memory._pointer = (byte*)bytes;
		memory._handle = default;
		memory._size = size;
	}

	private static void RentManagedFromPool(out PinnedMemory memory, int size, bool zero)
	{
		var array = BufferPool.Alloc(size, zero);
		var handle = GCHandle.Alloc(array, GCHandleType.Pinned);
		var pointer = (byte*)handle.AddrOfPinnedObject();

		memory._pointer = pointer;
		memory._handle = handle;
		memory._size = size;
	}

	/// <summary>Fill allocated block of memory with zeros.</summary>
	public void Clear()
	{
		if (_size <= 0 || _pointer is null) return;

		Mem.Zero(_pointer, _size);
	}

	/// <summary>
	/// Releases the memory.
	/// </summary>
	public void Free()
	{
		if (_handle.IsAllocated)
		{
			// might have more logic at some point
			ReleaseManaged();
		}
		else if (_pointer is not null)
		{
			ReleaseNative();
		}

		ClearFields();
	}

	private void ReleaseManaged()
	{
		var array = _handle.IsAllocated ? (byte[]?)_handle.Target : null;
		_handle.Free();
		BufferPool.Free(array);
	}

	private void ReleaseNative()
	{
		GC.RemoveMemoryPressure(_size);
		Mem.Free(_pointer);
	}

	private void ClearFields()
	{
		_pointer = null;
		_handle = default;
		_size = 0;
	}
}
