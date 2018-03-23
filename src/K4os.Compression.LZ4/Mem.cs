using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace K4os.Compression.LZ4
{
	public unsafe class Mem
	{
		public const int K1 = 1024;
		public const int K64 = 0x10000;
		public const int K256 = 256 * K1;
		public const int M1 = 1024 * K1;
		public const int M4 = M1 * 4;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static int RoundUp(int value, int step) => (value + step - 1) / step * step;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void Copy(byte* target, byte* source, int length)
		{
			// Buffer.MemoryCopy(source, target, length, length);
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
				*(uint*) target = *(ushort*) source;
				target += sizeof(ushort);
				source += sizeof(ushort);
				length -= sizeof(ushort);
			}

			if (length > 0)
			{
				*target = *source;
				// target++; source++; length--;
			}
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static void CopyBackwards(byte* target, byte* source, int length)
		{
			target += length;
			source += length;

			while (length >= sizeof(ulong))
			{
				target -= sizeof(ulong);
				source -= sizeof(ulong);
				*(ulong*) target = *(ulong*) source;
				length -= sizeof(ulong);
			}

			if (length >= sizeof(uint))
			{
				target -= sizeof(uint);
				source -= sizeof(uint);
				*(uint*) target = *(uint*) source;
				length -= sizeof(uint);
			}

			if (length >= sizeof(ushort))
			{
				target -= sizeof(ushort);
				source -= sizeof(ushort);
				*(ushort*) target = *(ushort*) source;
				length -= sizeof(ushort);
			}

			if (length > 0)
			{
				target--;
				source--;
				*target = *source;
				// length--;
			}
		}

		/// <summary>
		/// Compies memory block for <paramref name="source"/> to <paramref name="target"/> 
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

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void Move(byte* target, byte* source, int length)
		{
			var diff = target - source;

			if (diff < 0 || diff >= length)
			{
				Copy(target, source, length);
			}
			else
			{
				CopyBackwards(target, source, length);
			}
		}

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
				// target++; length--;
			}
		}

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
				// target++; length--;
			}
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void Copy8(byte* target, byte* source)
		{
			*((ulong*) (target + 0)) = *((ulong*) (source + 0));
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void Copy16(byte* target, byte* source)
		{
			*((ulong*) (target + 0)) = *((ulong*) (source + 0));
			*((ulong*) (target + 8)) = *((ulong*) (source + 8));
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void Copy18(byte* target, byte* source)
		{
			*((ulong*) (target + 0)) = *((ulong*) (source + 0));
			*((ulong*) (target + 8)) = *((ulong*) (source + 8));
			*((ushort*) (target + 16)) = *((ushort*) (source + 16));
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void* Alloc(int size) => Marshal.AllocHGlobal(size).ToPointer();

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void* AllocZero(int size)
		{
			var result = Alloc(size);
			Zero((byte*) result, size);
			return result;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void Free(void* ptr) => Marshal.FreeHGlobal(new IntPtr(ptr));

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		protected static byte Peek8(void* p) => *(byte*) p;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		protected static ushort Peek16(void* p) => *(ushort*) p;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		protected static uint Peek32(void* p) => *(uint*) p;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		protected static ulong Peek64(void* p) => *(ulong*) p;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		protected static void Poke8(void* p, byte v) => *(byte*) p = v;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		protected static void Poke16(void* p, ushort v) => *(ushort*) p = v;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		protected static void Poke32(void* p, uint v) => *(uint*) p = v;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		protected static void Poke64(void* p, ulong v) => *(ulong*) p = v;
	}
}
