using System.Runtime.CompilerServices;

namespace K4os.Compression.LZ4.Internal
{
	public unsafe class Mem64: Mem
	{
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

		/// <summary>Reads exactly 8 bytes from given address.</summary>
		/// <param name="p">Address.</param>
		/// <returns>8 bytes at given address.</returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static ulong Peek64(void* p) => *(ulong*) p;

		/// <summary>Writes exactly 8 bytes to given address.</summary>
		/// <param name="p">Address.</param>
		/// <param name="v">Value.</param>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void Poke64(void* p, ulong v) => *(ulong*) p = v;

		/// <summary>Copies exactly 8 bytes from source to target.</summary>
		/// <param name="target">Target address.</param>
		/// <param name="source">Source address.</param>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void Copy8(byte* target, byte* source)
		{
			*(ulong*) (target + 0) = *(ulong*) (source + 0);
		}

		/// <summary>Copies exactly 16 bytes from source to target.</summary>
		/// <param name="target">Target address.</param>
		/// <param name="source">Source address.</param>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void Copy16(byte* target, byte* source)
		{
			*(ulong*) (target + 0) = *(ulong*) (source + 0);
			*(ulong*) (target + 8) = *(ulong*) (source + 8);
		}

		/// <summary>Copies exactly 18 bytes from source to target.</summary>
		/// <param name="target">Target address.</param>
		/// <param name="source">Source address.</param>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void Copy18(byte* target, byte* source)
		{
			*(ulong*) (target + 0) = *((ulong*) (source + 0));
			*(ulong*) (target + 8) = *((ulong*) (source + 8));
			*(ushort*) (target + 16) = *((ushort*) (source + 16));
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
		public static void WildCopy8(byte* target, byte* source, void* limit)
		{
			do
			{
				*(ulong*) target = *(ulong*) source;
				target += sizeof(ulong);
				source += sizeof(ulong);
			}
			while (target < limit);
		}

		/// <summary>
		/// Copies memory block for <paramref name="source"/> to <paramref name="target"/>
		/// up to (around) <paramref name="limit"/>.
		/// It does not handle overlapping blocks and may copy up to 32 bytes more than expected.
		/// </summary>
		/// <param name="target">The target block address.</param>
		/// <param name="source">The source block address.</param>
		/// <param name="limit">The limit (in target block).</param>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void WildCopy32(byte* target, byte* source, void* limit)
		{
			const int step = sizeof(ulong) * 4;
			do
			{
				*(ulong*) (target + 0) = *(ulong*) (source + 0);
				*(ulong*) (target + 8) = *(ulong*) (source + 8);
				*(ulong*) (target + 16) = *(ulong*) (source + 16);
				*(ulong*) (target + 24) = *(ulong*) (source + 24);
				source += step;
				target += step;
			}
			while (target < limit);
		}
		
		/// <summary>
		/// Copies memory block for <paramref name="source"/> to <paramref name="target"/>.
		/// This is proper implementation of memcpy (with all weird behaviour for overlapping blocks).
		/// It is slower than "Copy" but may be required if "Copy" causes problems.
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
	}
}
