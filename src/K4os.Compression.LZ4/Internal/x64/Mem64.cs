// ReSharper disable ClassNeverInstantiated.Global
// ReSharper disable AccessToStaticMemberViaDerivedType

using System.Runtime.CompilerServices;

namespace K4os.Compression.LZ4.Internal
{
	/// <summary>Unsafe memory operations.</summary>
	#if !BIT32
	public unsafe class Mem64: Mem
	#else
	public unsafe class Mem32: Mem
	#endif
	{
		/// <summary>Reads exactly 8 bytes from given address.</summary>
		/// <param name="p">Address.</param>
		/// <returns>8 bytes at given address.</returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static ulong _Peek8(void* p)
		{
			#if !BIT32
			return *(ulong*) p;
			#else
			return *((uint*) p + 0) | (ulong) *((uint*) p + 1) << 32;
			#endif
		}

		/// <summary>Writes exactly 8 bytes to given address.</summary>
		/// <param name="p">Address.</param>
		/// <param name="v">Value.</param>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void _Poke8(void* p, ulong v)
		{
			#if !BIT32
			*(ulong*) p = v;
			#else
			*((uint*) p + 0) = (uint) v;
			*((uint*) p + 1) = (uint) (v >> 32);
			#endif
		}
		
		#if !BIT32

		/// <summary>Reads 8 bytes from given address.</summary>
		/// <param name="p">Address.</param>
		/// <returns>8 bytes at given address.</returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static ulong PeekW(void* p) => _Peek8(p);

		/// <summary>Writes 8 bytes to given address.</summary>
		/// <param name="p">Address.</param>
		/// <param name="v">Value.</param>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void PokeW(void* p, ulong v) => _Poke8(p, v);
		
		#else

		/// <summary>Reads 4 bytes from given address.</summary>
		/// <param name="p">Address.</param>
		/// <returns>4 bytes at given address.</returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static uint PeekW(void* p) => Peek4(p);

		/// <summary>Writes 4 or 8 bytes to given address.</summary>
		/// <param name="p">Address.</param>
		/// <param name="v">Value.</param>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void PokeW(void* p, uint v) => Poke4(p, v);

		#endif

		/// <summary>Fills memory block with repeating pattern of a single byte.</summary>
		/// <param name="target">Address.</param>
		/// <param name="value">A pattern.</param>
		/// <param name="length">Length.</param>
		/// <returns>Original pointer.</returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public new static byte* Fill(byte* target, byte value, int length)
		{
			var baseline = target;
			
			var value8 = (ulong) value;
			value8 |= value8 << 8;
			value8 |= value8 << 16;

			#if !BIT32
			
			value8 |= value8 << 32;
			
			while (length >= sizeof(ulong))
			{
				_Poke8(target, value8);
				target += sizeof(ulong);
				length -= sizeof(ulong);
			}
			
			#endif

			while (length >= sizeof(uint))
			{
				Poke4(target, (uint) value8);
				target += sizeof(uint);
				length -= sizeof(uint);
			}

			if (length >= sizeof(ushort))
			{
				Poke2(target, (ushort) value8);
				target += sizeof(ushort);
				length -= sizeof(ushort);
			}

			if (length > 0)
			{
				Poke1(target, (byte) value8);
				// target++;
			}

			return baseline;
		}

		/// <summary>Copies exactly 8 bytes from source to target.</summary>
		/// <param name="target">Target address.</param>
		/// <param name="source">Source address.</param>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void Copy8(byte* target, byte* source)
		{
			#if !BIT32
			*(ulong*) target = *(ulong*) source;
			#else
			var temp = Peek4(source + 4);
			Copy4(target, source);
			Poke4(target + 4, temp);
			#endif
		}

		/// <summary>Copies exactly 16 bytes from source to target.</summary>
		/// <param name="target">Target address.</param>
		/// <param name="source">Source address.</param>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void Copy16(byte* target, byte* source)
		{
			Copy8(target + 0, source + 0);
			Copy8(target + 8, source + 8);
		}

		/// <summary>Copies exactly 18 bytes from source to target.</summary>
		/// <param name="target">Target address.</param>
		/// <param name="source">Source address.</param>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void Copy18(byte* target, byte* source)
		{
			Copy8(target + 0, source + 0);
			Copy8(target + 8, source + 8);
			Copy2(target + 16, source + 16);
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
				Copy8(source, target);
				target += sizeof(ulong);
				source += sizeof(ulong);
			}
			while (target < limit);
		}
	}
}
