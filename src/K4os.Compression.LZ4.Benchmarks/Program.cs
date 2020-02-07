using System;
using System.Runtime.CompilerServices;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;

namespace K4os.Compression.LZ4.Benchmarks
{
	class Program
	{
		static void Main(string[] args)
		{
			BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(args);
		}
	}

	[DisassemblyDiagnoser(printAsm: true, printSource: true)]
	[LegacyJitX86Job]
	[RyuJitX64Job]
	[RyuJitX86Job]
	[MonoJob]
	public unsafe class ConditionalBranches
	{
		public static readonly int ARCH = IntPtr.Size;

		#pragma warning disable 414
		private int _global;
		#pragma warning restore 414

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void Copy2(byte* target, byte* source) =>
			*(ushort*) target = *(ushort*) source;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void Poke4(void* p, uint v) => *(uint*) p = v;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static uint Peek4(void* p) => *(uint*) p;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void Copy4(byte* target, byte* source) =>
			*(uint*) target = *(uint*) source;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void Copy8(byte* target, byte* source)
		{
			if (ARCH >= 8)
			{
				*(ulong*) target = *(ulong*) source;
			}
			else
			{
				var temp = Peek4(source + 4);
				Copy4(target, source);
				Poke4(target + 4, temp);
			}
		}

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

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void ConditionalMethod()
		{
			var buffer = stackalloc byte[1024];
			Copy18(buffer, buffer + 128);
			_global = 0x1234;
			WildCopy8(buffer, buffer + 128, buffer + 128);
			_global = 0x4321;
		}

		[Benchmark]
		public void ExecuteConditionalMethod() { ConditionalMethod(); }
	}
}
