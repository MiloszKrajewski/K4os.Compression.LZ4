using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace K4os.Compression.LZ4
{
	internal unsafe class Mem
	{
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void Copy(byte* target, byte* source, int length)
		{
			while (length > sizeof(ulong))
			{
				*(ulong*) target = *(ulong*) source;
				target += sizeof(ulong);
				source += sizeof(ulong);
				length -= sizeof(ulong);
			}

			while (length > sizeof(uint))
			{
				*(uint*) target = *(uint*) source;
				target += sizeof(uint);
				source += sizeof(uint);
				length -= sizeof(uint);
			}

			while (length > 0)
			{
				*target = *source;
				target++;
				source++;
				length--;
			}
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void WildCopy(byte* d, byte* s, void* e)
		{
			do
			{
				*(ulong*) d = *(ulong*) s;
				d += sizeof(ulong);
				s += sizeof(ulong);
			}
			while (d < e);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void Move(byte* target, byte* source, int length)
		{
			var diff = target - source;

			if (diff < 0 || diff >= length)
			{
				while (length > sizeof(ulong))
				{
					*(ulong*) target = *(ulong*) source;
					target += sizeof(ulong);
					source += sizeof(ulong);
					length -= sizeof(ulong);
				}

				while (length > sizeof(uint))
				{
					*(uint*) target = *(uint*) source;
					target += sizeof(uint);
					source += sizeof(uint);
					length -= sizeof(uint);
				}

				while (length > 0)
				{
					*target = *source;
					target++;
					source++;
					length--;
				}
			}
			else
			{
				target += length;
				source += length;

				while (length > sizeof(ulong))
				{
					target -= sizeof(ulong);
					source -= sizeof(ulong);
					*(ulong*) target = *(ulong*) source;
					length -= sizeof(ulong);
				}

				while (length > sizeof(uint))
				{
					target -= sizeof(uint);
					source -= sizeof(uint);
					*(uint*) target = *(uint*) source;
					length -= sizeof(uint);
				}

				while (length > 0)
				{
					target--;
					source--;
					*target = *source;
					length--;
				}
			}
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void Zero(byte* target, int length)
		{
			while (length > sizeof(ulong))
			{
				*(ulong*) target = 0;
				target += sizeof(ulong);
				length -= sizeof(ulong);
			}

			while (length > sizeof(uint))
			{
				*(uint*) target = 0;
				target += sizeof(uint);
				length -= sizeof(uint);
			}

			while (length > 0)
			{
				*target = 0;
				target++;
				length--;
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
		public static void Free(void *ptr) => Marshal.FreeHGlobal(new IntPtr(ptr));
	}
}
