// ReSharper disable AccessToStaticMemberViaDerivedType

using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using K4os.Compression.LZ4.Internal;

namespace TestSnippets
{
	public static unsafe class Program
	{
		public static void Main()
		{
			var buffer = Mem.Alloc(1024);
			try
			{
				Breakpoint(0xdeadc0de);

				WildCopy8_B((byte*) buffer, (byte*)buffer + 55, (byte*)buffer + 100);

				Breakpoint(0xdeadbeef);

				// Copy18_B((byte*)buffer, (byte*) buffer + 55);
				//
				// Breakpoint(0x12341234);
			}
			finally
			{
				Mem.Free(buffer);
			}
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void Copy18_A(byte* target, byte* source)
		{
			*(ulong*) (target + 0) = *((ulong*) (source + 0));
			*(ulong*) (target + 8) = *((ulong*) (source + 8));
			*(ushort*) (target + 16) = *((ushort*) (source + 16));
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void WildCopy8_A(byte* target, byte* source, void* limit)
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
		public static void WildCopy8_B(byte* target, byte* source, void* limit)
		{
			do
			{
				Copy8(target, source);
				target += sizeof(ulong);
				source += sizeof(ulong);
			}
			while (target < limit);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void Copy8(byte* target, byte* source)
		{
			Mem64.Poke64(target, Mem64.Peek64(source));
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void Copy16(byte* target, byte* source)
		{
			Copy8(target + 0, source + 0);
			Copy8(target + 8, source + 8);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void Copy18_B(byte* target, byte* source)
		{
			Copy16(target + 0, source + 0);
			*(ushort*) (target + 16) = *((ushort*) (source + 16));
		}

		private static void Breakpoint(uint p0)
		{
			Console.WriteLine(">>> {0}", p0);
			Debugger.Break();
		}
	}
}
