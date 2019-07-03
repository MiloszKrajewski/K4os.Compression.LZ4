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

		/// <summary>
		/// Copies memory block for <paramref name="source"/> to <paramref name="target"/> 
		/// up to (around) <paramref name="limit"/>.
		/// It does not handle overlapping blocks and may copy up to 8 bytes more than expected.
		/// </summary>
		/// <param name="target">The target block address.</param>
		/// <param name="source">The source block address.</param>
		/// <param name="limit">The limit (in target block).</param>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void WildCopy(byte* target, byte* source, void* limit)
		{
			do
			{
				*(ulong*) target = *(ulong*) source;
				target += sizeof(ulong);
				source += sizeof(ulong);
			}
			while (target < limit);
		}

		/// <summary>Fill block of memory with zeroes.</summary>
		/// <param name="target">Address.</param>
		/// <param name="length">Length.</param>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void Zero(byte* target, int length)
		{
			while (length >= sizeof(ulong))
			{
				*(ulong*) target = 0;
				target += sizeof(ulong);
				length -= sizeof(ulong);
			}

			if (length >= sizeof(uint))
			{
				*(uint*) target = 0;
				target += sizeof(uint);
				length -= sizeof(uint);
			}

			if (length >= sizeof(ushort))
			{
				*(ushort*) target = 0;
				target += sizeof(ushort);
				length -= sizeof(ushort);
			}

			if (length > 0)
			{
				*target = 0;
			}
		}

		/// <summary>Fills memory block with repeating pattern of a single byte.</summary>
		/// <param name="target">Address.</param>
		/// <param name="value">A pattern.</param>
		/// <param name="length">Length.</param>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void Fill(byte* target, byte value, int length)
		{
			var value8 = (ulong) value;
			value8 |= value8 << 8;
			value8 |= value8 << 16;
			value8 |= value8 << 32;

			while (length >= sizeof(ulong))
			{
				*(ulong*) target = value8;
				target += sizeof(ulong);
				length -= sizeof(ulong);
			}

			if (length >= sizeof(uint))
			{
				*(uint*) target = (uint) value8;
				target += sizeof(uint);
				length -= sizeof(uint);
			}

			if (length >= sizeof(ushort))
			{
				*(ushort*) target = (ushort) value8;
				target += sizeof(ushort);
				length -= sizeof(ushort);
			}

			if (length > 0)
			{
				*target = value;
			}
		}

		/// <summary>
		/// Copies memory block for <paramref name="source"/> to <paramref name="target"/>.
		/// This is proper implementation of memcpy (with all then weird behaviour for
		/// overlapping blocks). It is slower than "Copy" but may be required if "Copy"
		/// causes problems.
		/// </summary>
		/// <param name="target">The target block address.</param>
		/// <param name="source">The source block address.</param>
		/// <param name="length">Length in bytes.</param>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void LoopCopy(byte* target, byte* source, int length)
		{
			while (length >= sizeof(ulong))
			{
				*(ulong*) target = *(ulong*) source;
				target += sizeof(ulong);
				source += sizeof(ulong);
				length -= sizeof(ulong);
			}

			if (length >= sizeof(uint))
			{
				*(uint*) target = *(uint*) source;
				target += sizeof(uint);
				source += sizeof(uint);
				length -= sizeof(uint);
			}

			if (length >= sizeof(ushort))
			{
				*(ushort*) target = *(ushort*) source;
				target += sizeof(ushort);
				source += sizeof(ushort);
				length -= sizeof(ushort);
			}

			if (length > 0)
			{
				*target = *source;
			}
		}

		/// <summary>
		/// Copies memory block backwards from <paramref name="source"/> to <paramref name="target"/>.
		/// This is needed to implement memmove It is slower than "Move" but is needed for .NET 4.5,
		/// which does not implement Buffer.MemoryCopy.
		/// </summary>
		/// <param name="target">The target block address.</param>
		/// <param name="source">The source block address.</param>
		/// <param name="length">Length in bytes.</param>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static void LoopCopyBack(byte* target, byte* source, int length)
		{
			if (length <= 0) return;

			target += length;
			source += length;

			while (length >= sizeof(ulong))
			{
				target -= sizeof(ulong);
				source -= sizeof(ulong);
				length -= sizeof(ulong);
				*(ulong*) target = *(ulong*) source;
			}

			if (length >= sizeof(uint))
			{
				target -= sizeof(uint);
				source -= sizeof(uint);
				length -= sizeof(uint);
				*(uint*) target = *(uint*) source;
			}

			if (length >= sizeof(ushort))
			{
				target -= sizeof(ushort);
				source -= sizeof(ushort);
				length -= sizeof(ushort);
				*(ushort*) target = *(ushort*) source;
			}

			if (length > 0)
			{
				target--;
				source--;
				*target = *source;
			}
		}

		/// <summary>
		/// Moves memory block for <paramref name="source"/> to <paramref name="target"/>.
		/// It handles overlapping block properly.
		/// </summary>
		/// <param name="target">The target block address.</param>
		/// <param name="source">The source block address.</param>
		/// <param name="length">Length in bytes.</param>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void LoopMove(byte* target, byte* source, int length)
		{
			if (length <= 0 || source == target)
				return;

			if (source >= target || source + length <= target)
			{
				LoopCopy(target, source, length);
			}
			else
			{
				LoopCopyBack(target, source, length);
			}
		}

		/// <summary>Copies exactly 8 bytes from source to target.</summary>
		/// <param name="target">Target address.</param>
		/// <param name="source">Source address.</param>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void Copy8(byte* target, byte* source)
		{
			*((ulong*) (target + 0)) = *((ulong*) (source + 0));
		}

		/// <summary>Copies exactly 16 bytes from source to target.</summary>
		/// <param name="target">Target address.</param>
		/// <param name="source">Source address.</param>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void Copy16(byte* target, byte* source)
		{
			*((ulong*) (target + 0)) = *((ulong*) (source + 0));
			*((ulong*) (target + 8)) = *((ulong*) (source + 8));
		}

		/// <summary>Copies exactly 18 bytes from source to target.</summary>
		/// <param name="target">Target address.</param>
		/// <param name="source">Source address.</param>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void Copy18(byte* target, byte* source)
		{
			*((ulong*) (target + 0)) = *((ulong*) (source + 0));
			*((ulong*) (target + 8)) = *((ulong*) (source + 8));
			*((ushort*) (target + 16)) = *((ushort*) (source + 16));
		}

		/// <summary>Allocated block of memory. It is NOT initialized with zeroes.</summary>
		/// <param name="size">Size in bytes.</param>
		/// <returns>Pointer to allocated block.</returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void* Alloc(int size) =>
			Marshal.AllocHGlobal(size).ToPointer();

		/// <summary>Allocated block of memory and fills it with zeroes.</summary>
		/// <param name="size">Size in bytes.</param>
		/// <returns>Pointer to allocated block.</returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void* AllocZero(int size)
		{
			var result = Alloc(size);
			Zero((byte*) result, size);
			return result;
		}

		/// <summary>
		/// Free memory allocated previously with <see cref="Alloc"/> or <see cref="AllocZero"/>
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

		/// <summary>Reads exactly 8 bytes from given address.</summary>
		/// <param name="p">Address.</param>
		/// <returns>8 bytes at given address.</returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static ulong Peek64(void* p) => *(ulong*) p;

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

		/// <summary>Writes exactly 8 bytes to given address.</summary>
		/// <param name="p">Address.</param>
		/// <param name="v">Value.</param>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void Poke64(void* p, ulong v) => *(ulong*) p = v;
	}
}
