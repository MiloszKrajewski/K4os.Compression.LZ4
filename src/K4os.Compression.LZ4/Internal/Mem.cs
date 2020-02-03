using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace K4os.Compression.LZ4.Internal
{
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
		#if NET45
		public static readonly byte[] Empty = new byte[0];
		#else
		public static readonly byte[] Empty = Array.Empty<byte>();
		#endif


		/// <summary>Rounds integer value up to nearest multiple of step.</summary>
		/// <param name="value">A value.</param>
		/// <param name="step">A step.</param>
		/// <returns>Value rounded up.</returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static int RoundUp(int value, int step) => (value + step - 1) / step * step;

		/// <summary>
		/// Copies memory block for <paramref name="source"/> to <paramref name="target"/>.
		/// Even though it is called "copy" it actually behaves like "move" which
		/// might be potential problem, although it shouldn't as I cannot think about
		/// any situation when "copy" invalid behaviour (forward copy of overlapping blocks)
		/// can be a desired.
		/// </summary>
		/// <param name="target">The target block address.</param>
		/// <param name="source">The source block address.</param>
		/// <param name="length">Length in bytes.</param>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void Copy(byte* target, byte* source, int length)
		{
			#if !NET45
			Buffer.MemoryCopy(source, target, length, length);
			#else
			if (length <= 0) return;
			Unsafe.CopyBlock(target, source, (uint) length);
			#endif
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
			#if !NET45
			Buffer.MemoryCopy(source, target, length, length);
			#else
			if (length <= 0) return;
			Unsafe.CopyBlock(target, source, (uint) length);
			#endif
		}

		/// <summary>Allocated block of memory. It is NOT initialized with zeroes.</summary>
		/// <param name="size">Size in bytes.</param>
		/// <returns>Pointer to allocated block.</returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void* Alloc(int size) =>
			Marshal.AllocHGlobal(size).ToPointer();

		/// <summary>
		/// Free memory allocated previously with <see cref="Alloc"/>.
		/// </summary>
		/// <param name="ptr"></param>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void Free(void* ptr) => Marshal.FreeHGlobal(new IntPtr(ptr));

		/// <summary>Reads exactly 1 byte from given address.</summary>
		/// <param name="p">Address.</param>
		/// <returns>Byte at given address.</returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static byte Peek8(void* p) => *(byte*) p;

		/// <summary>Reads exactly 2 bytes from given address.</summary>
		/// <param name="p">Address.</param>
		/// <returns>2 bytes at given address.</returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static ushort Peek16(void* p) => *(ushort*) p;

		/// <summary>Reads exactly 4 bytes from given address.</summary>
		/// <param name="p">Address.</param>
		/// <returns>4 bytes at given address.</returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static uint Peek32(void* p) => *(uint*) p;

		/// <summary>Writes exactly 1 byte to given address.</summary>
		/// <param name="p">Address.</param>
		/// <param name="v">Value.</param>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void Poke8(void* p, byte v) => *(byte*) p = v;

		/// <summary>Writes exactly 2 bytes to given address.</summary>
		/// <param name="p">Address.</param>
		/// <param name="v">Value.</param>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void Poke16(void* p, ushort v) => *(ushort*) p = v;

		/// <summary>Writes exactly 4 bytes to given address.</summary>
		/// <param name="p">Address.</param>
		/// <param name="v">Value.</param>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void Poke32(void* p, uint v) => *(uint*) p = v;
	}
}
