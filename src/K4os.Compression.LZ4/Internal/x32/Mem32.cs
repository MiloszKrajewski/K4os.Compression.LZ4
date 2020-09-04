//------------------------------------------------------------------------------
//
// This file has been generated. All changes will be lost.
//
//------------------------------------------------------------------------------
#define BIT32

using System;
using System.Runtime.CompilerServices;

namespace K4os.Compression.LZ4.Internal
{
	/// <summary>Unsafe memory operations.</summary>
	#if BIT32
	public unsafe class Mem32: Mem
	#else
	public unsafe class Mem64: Mem
	#endif
	{
		#if !BIT32
		
		/// <summary>Reads exactly 2 bytes from given address.</summary>
		/// <param name="p">Address.</param>
		/// <returns>2 bytes at given address.</returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public new static ushort Peek2(void* p) => *(ushort*) p;

		/// <summary>Writes exactly 2 bytes to given address.</summary>
		/// <param name="p">Address.</param>
		/// <param name="v">Value.</param>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public new static void Poke2(void* p, ushort v) => *(ushort*) p = v;

		/// <summary>Reads exactly 4 bytes from given address.</summary>
		/// <param name="p">Address.</param>
		/// <returns>4 bytes at given address.</returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public new static uint Peek4(void* p) => *(uint*) p;

		/// <summary>Writes exactly 4 bytes to given address.</summary>
		/// <param name="p">Address.</param>
		/// <param name="v">Value.</param>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public new static void Poke4(void* p, uint v) => *(uint*) p = v;

		/// <summary>Copies exactly 1 byte from source to target.</summary>
		/// <param name="target">Target address.</param>
		/// <param name="source">Source address.</param>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public new static void Copy1(byte* target, byte* source) =>
			*target = *source;

		/// <summary>Copies exactly 2 bytes from source to target.</summary>
		/// <param name="target">Target address.</param>
		/// <param name="source">Source address.</param>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public new static void Copy2(byte* target, byte* source) =>
			*(ushort*) target = *(ushort*) source;

		/// <summary>Copies exactly 4 bytes from source to target.</summary>
		/// <param name="target">Target address.</param>
		/// <param name="source">Source address.</param>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public new static void Copy4(byte* target, byte* source) =>
			*(uint*) target = *(uint*) source;

		/// <summary>Reads exactly 8 bytes from given address.</summary>
		/// <param name="p">Address.</param>
		/// <returns>8 bytes at given address.</returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public new static ulong Peek8(void* p) => *(ulong*) p;

		/// <summary>Writes exactly 8 bytes to given address.</summary>
		/// <param name="p">Address.</param>
		/// <param name="v">Value.</param>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public new static void Poke8(void* p, ulong v) => *(ulong*) p = v;

		/// <summary>Copies exactly 8 bytes from source to target.</summary>
		/// <param name="target">Target address.</param>
		/// <param name="source">Source address.</param>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public new static void Copy8(byte* target, byte* source) =>
			*(ulong*) target = *(ulong*) source;

		#endif

		#if BIT32
		
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

		#else

		/// <summary>Reads 8 bytes from given address.</summary>
		/// <param name="p">Address.</param>
		/// <returns>8 bytes at given address.</returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static ulong PeekW(void* p) => Peek8(p);

		/// <summary>Writes 8 bytes to given address.</summary>
		/// <param name="p">Address.</param>
		/// <param name="v">Value.</param>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void PokeW(void* p, ulong v) => Poke8(p, v);

		#endif

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
				Copy8(target, source);
				target += sizeof(ulong);
				source += sizeof(ulong);
			}
			while (target < limit);
		}

		/// <summary>
		/// Copies memory block for <paramref name="source"/> to <paramref name="target"/>
		/// up to (around) <paramref name="limit"/>.
		/// It does not handle overlapping blocks and may copy up to 32 bytes more than expected.
		/// This version copies two times 16 bytes (instead of one time 32 bytes)
		/// because it must be compatible with offsets >= 16.
		/// </summary>
		/// <param name="target">The target block address.</param>
		/// <param name="source">The source block address.</param>
		/// <param name="limit">The limit (in target block).</param>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void WildCopy32(byte* target, byte* source, void* limit)
		{
			do
			{
				Copy16(target + 0, source + 0);
				Copy16(target + 16, source + 16);
				target += 32;
				source += 32;
			}
			while (target < limit);
		}
	}
}
