using System.Runtime.CompilerServices;

namespace K4os.Compression.LZ4.Internal
{
	public unsafe class Mem32: Mem
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
				var temp = *(uint*) source;
				*(uint*) (target + sizeof(uint)) = *(uint*) (source + sizeof(uint));
				*(uint*) target = temp;
				target += sizeof(ulong);
				source += sizeof(ulong);
			}
			while (target < limit);
		}

		/// <summary>
		/// Copies memory block for <paramref name="source"/> to <paramref name="target"/>.
		/// This is proper implementation of memcpy (with all weir behaviour for overlapping blocks).
		/// It is slower than "Copy" but may be required if "Copy" causes problems.
		/// </summary>
		/// <param name="target">The target block address.</param>
		/// <param name="source">The source block address.</param>
		/// <param name="length">Length in bytes.</param>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void LoopCopy(byte* target, byte* source, int length)
		{
			while (length >= sizeof(uint))
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
			while (length >= sizeof(uint))
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
			var value8 = (uint) value;
			value8 |= value8 << 8;
			value8 |= value8 << 16;

			while (length >= sizeof(uint))
			{
				*(uint*) target = value8;
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
	}
}
