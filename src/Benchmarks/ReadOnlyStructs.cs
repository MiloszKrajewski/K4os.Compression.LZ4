using System.Runtime.CompilerServices;
using BenchmarkDotNet.Attributes;

namespace Benchmarks
{
	public class ReadOnlyStructs
	{
		// ReSharper disable once NotAccessedField.Local
		private int _sink;

		[Benchmark]
		public void UseStructAIn()
		{
			var a = CreateA();
			Consume(ConsumeAIn(a));
		}

		[Benchmark]
		public void UseStructAInline()
		{
			var a = CreateA();
			Consume(ConsumeAInline(a));
		}
		
		[Benchmark]
		public void UseStructB()
		{
			var b = CreateB();
			Consume(ConsumeB(b));
		}

		[Benchmark]
		public void UseStructBIn()
		{
			var b = CreateB();
			Consume(ConsumeBIn(b));
		}

		private static StructA CreateA() { return new StructA(5, 2); }
		private static StructB CreateB() { return new StructB(5, 2); }
		
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static int ConsumeAInline(in StructA a) { return ConsumeAInline0(a); }
		private static int ConsumeAIn(in StructA a) { return ConsumeAIn0(a); }
		private static int ConsumeB(StructB b) { return ConsumeB0(b); }
		private static int ConsumeBIn(in StructB b) { return ConsumeBIn0(b); }

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static int ConsumeAInline0(in StructA a) { return a.A * a.B; }
		private static int ConsumeAIn0(in StructA a) { return a.A * a.B; }
		private static int ConsumeB0(StructB b) { return b.A * b.B; }
		private static int ConsumeBIn0(in StructB b) { return b.A * b.B; }

		private void Consume(int value) { _sink = value; }
	}

	public readonly struct StructA
	{
		public int A { get; }
		public int B { get; }

		public StructA(int a, int b)
		{
			A = a;
			B = b;
		}
	}

	public struct StructB
	{
		public int A { get; set;  }
		public int B { get; set; }

		public StructB(int a, int b)
		{
			A = a;
			B = b;
		}
	}
}
