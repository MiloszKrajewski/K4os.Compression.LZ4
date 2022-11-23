using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace K4os.Compression.LZ4.Internal;

/// <summary>Utility class with memory related functions.</summary>
public unsafe class Mem
{
	/// <summary>1 KiB</summary>
	public const int K1 = 1024;

	/// <summary>2 KiB</summary>
	public const int K2 = 2 * K1;

	/// <summary>4 KiB</summary>
	public const int K4 = 4 * K1;

	/// <summary>8 KiB</summary>
	public const int K8 = 8 * K1;

	/// <summary>16 KiB</summary>
	public const int K16 = 16 * K1;

	/// <summary>32 KiB</summary>
	public const int K32 = 32 * K1;

	/// <summary>64 KiB</summary>
	public const int K64 = 64 * K1;

	/// <summary>128 KiB</summary>
	public const int K128 = 128 * K1;

	/// <summary>256 KiB</summary>
	public const int K256 = 256 * K1;

	/// <summary>512 KiB</summary>
	public const int K512 = 512 * K1;

	/// <summary>1 MiB</summary>
	public const int M1 = 1024 * K1;

	/// <summary>4 MiB</summary>
	public const int M4 = 4 * M1;

	/// <summary>Empty byte array.</summary>
	public static readonly byte[] Empty = Array.Empty<byte>();

	/// <summary>Checks if process is ran in 32-bit mode.</summary>
	public static bool System32
	{
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => sizeof(void*) < sizeof(ulong);
	}

	/// <summary>Rounds integer value up to nearest multiple of step.</summary>
	/// <param name="value">A value.</param>
	/// <param name="step">A step.</param>
	/// <returns>Value rounded up.</returns>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static int RoundUp(int value, int step) => (value + step - 1) / step * step;
		
	/// <summary>
	/// Copies memory block for <paramref name="source"/> to <paramref name="target"/>.
	/// </summary>
	/// <param name="target">The target block address.</param>
	/// <param name="source">The source block address.</param>
	/// <param name="length">Length in bytes.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal static void CpBlk(void* target, void* source, uint length) => 
		Unsafe.CopyBlockUnaligned(target, source, length);

	/// <summary>
	/// Fills <paramref name="target"/> memory block with predefined <paramref name="value"/>.
	/// </summary>
	/// <param name="target">The target block address.</param>
	/// <param name="value">Value to be used.</param>
	/// <param name="length">Length in bytes.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal static void ZBlk(void* target, byte value, uint length) => 
		Unsafe.InitBlockUnaligned(target, value, length);

	/// <summary>
	/// Copies memory block for <paramref name="source"/> to <paramref name="target"/>.
	/// </summary>
	/// <param name="target">The target block address.</param>
	/// <param name="source">The source block address.</param>
	/// <param name="length">Length in bytes.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static void Copy(byte* target, byte* source, int length)
	{
		if (length <= 0) return;
		CpBlk(target, source, (uint) length);
	}

	/// <summary>
	/// Copies memory block for <paramref name="source"/> to <paramref name="target"/>.
	/// It handle "move" semantic properly handling overlapping blocks properly.
	/// </summary>
	/// <param name="target">The target block address.</param>
	/// <param name="source">The source block address.</param>
	/// <param name="length">Length in bytes.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static void Move(byte* target, byte* source, int length)
	{
		Buffer.MemoryCopy(source, target, length, length);
	}
		
	/// <summary>Allocated block of memory. It is NOT initialized with zeroes.</summary>
	/// <param name="size">Size in bytes.</param>
	/// <returns>Pointer to allocated block.</returns>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static void* Alloc(int size) =>
		Marshal.AllocHGlobal(size).ToPointer();

	/// <summary>Fill block of memory with zeroes.</summary>
	/// <param name="target">Address.</param>
	/// <param name="length">Length.</param>
	/// <returns>Original pointer.</returns>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static byte* Zero(byte* target, int length) => Fill(target, 0, length);

	/// <summary>Fills memory block with repeating pattern of a single byte.</summary>
	/// <param name="target">Address.</param>
	/// <param name="value">A pattern.</param>
	/// <param name="length">Length.</param>
	/// <returns>Original pointer.</returns>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static byte* Fill(byte* target, byte value, int length)
	{
		if (length > 0) ZBlk(target, value, (uint) length);
		return target;
	}

	/// <summary>Allocates block of memory and fills it with zeroes.</summary>
	/// <param name="size">Size in bytes.</param>
	/// <returns>Pointer to allocated block.</returns>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static void* AllocZero(int size) =>
		Zero((byte*) Alloc(size), size);

	/// <summary>Free memory allocated previously with <see cref="Alloc"/>.</summary>
	/// <param name="ptr">Pointer to allocated block.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static void Free(void* ptr) => Marshal.FreeHGlobal(new IntPtr(ptr));
		
	/// <summary>Clones managed array to unmanaged one.
	/// Allows quicker yet less safe unchecked access.</summary>
	/// <param name="array">Input array.</param>
	/// <returns>Cloned array.</returns>
	public static T* CloneArray<T>(T[] array) where T: unmanaged
	{
		var length = Unsafe.SizeOf<T>() * array.Length;
		var target = Alloc(length);
		ref var source0 = ref Unsafe.As<T, byte>(ref array[0]);
			
		fixed (void* source = &source0)
			Copy((byte*) target, (byte*) source, length);

		return (T*)target;
	}

	/// <summary>Reads exactly 1 byte from given address.</summary>
	/// <param name="p">Address.</param>
	/// <returns>Byte at given address.</returns>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static byte Peek1(void* p) => *(byte*) p;

	/// <summary>Writes exactly 1 byte to given address.</summary>
	/// <param name="p">Address.</param>
	/// <param name="v">Value.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static void Poke1(void* p, byte v) => *(byte*) p = v;

	/// <summary>Reads exactly 2 bytes from given address.</summary>
	/// <param name="p">Address.</param>
	/// <returns>2 bytes at given address.</returns>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static ushort Peek2(void* p)
	{
		ushort result;
		CpBlk(&result, p, sizeof(ushort));
		return result;
	}

	/// <summary>Writes exactly 2 bytes to given address.</summary>
	/// <param name="p">Address.</param>
	/// <param name="v">Value.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static void Poke2(void* p, ushort v) => 
		CpBlk(p, &v, sizeof(ushort));

	/// <summary>Reads exactly 4 bytes from given address.</summary>
	/// <param name="p">Address.</param>
	/// <returns>4 bytes at given address.</returns>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static uint Peek4(void* p)
	{
		uint result;
		CpBlk(&result, p, sizeof(uint));
		return result;
	}
		
	/// <summary>Writes exactly 4 bytes to given address.</summary>
	/// <param name="p">Address.</param>
	/// <param name="v">Value.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static void Poke4(void* p, uint v) => 
		CpBlk(p, &v, sizeof(uint));

	/// <summary>Reads exactly 8 bytes from given address.</summary>
	/// <param name="p">Address.</param>
	/// <returns>8 bytes at given address.</returns>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static ulong Peek8(void* p)
	{
		ulong result;
		CpBlk(&result, p, sizeof(ulong));
		return result;
	}

	/// <summary>Writes exactly 8 bytes to given address.</summary>
	/// <param name="p">Address.</param>
	/// <param name="v">Value.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static void Poke8(void* p, ulong v) => 
		CpBlk(p, &v, sizeof(ulong));

	/// <summary>Copies exactly 1 byte from source to target.</summary>
	/// <param name="target">Target address.</param>
	/// <param name="source">Source address.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static void Copy1(byte* target, byte* source) =>
		*target = *source;

	/// <summary>Copies exactly 2 bytes from source to target.</summary>
	/// <param name="target">Target address.</param>
	/// <param name="source">Source address.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static void Copy2(byte* target, byte* source) =>
		CpBlk(target, source, 2);

	/// <summary>Copies exactly 4 bytes from source to target.</summary>
	/// <param name="target">Target address.</param>
	/// <param name="source">Source address.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static void Copy4(byte* target, byte* source) =>
		CpBlk(target, source, 4);

	/// <summary>Copies exactly 8 bytes from source to target.</summary>
	/// <param name="target">Target address.</param>
	/// <param name="source">Source address.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static void Copy8(byte* target, byte* source) =>
		CpBlk(target, source, 8);
}