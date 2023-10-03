using System;
using System.Buffers;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using BenchmarkDotNet.Attributes;

namespace Benchmarks
{
	[MemoryDiagnoser]
	public class BufferAllocation
	{
		private static readonly ArrayPool<byte> Pool = ArrayPool<byte>.Shared;

		[Params(32, 512, 1024, 0xFFFF)]
		public int Size { get; set; }
		
		[Params(512)]
		public int Threshold { get; set; }

		[Benchmark]
		public unsafe void NewByteArray()
		{
			var array = new byte[Size];
			fixed (byte* p = &array[0]) UseArray(p);
		}

		[Benchmark]
		public unsafe void PoolRentReturn()
		{
			var array = Pool.Rent(Size);
			fixed (byte* p = &array[0]) UseArray(p);
			Pool.Return(array);
		}
		
		[Benchmark]
		public unsafe void PoolRentReturnFinally()
		{
			var array = Pool.Rent(Size);
			try
			{
				fixed (byte* p = &array[0]) UseArray(p);
			}
			finally
			{
				Pool.Return(array);
			}
		}

		[Benchmark]
		public unsafe void NewByteArrayOrPool()
		{
			var array = SmartRent(Size);
			fixed (byte* p = &array[0]) UseArray(p);
			SmartReturn(array);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private byte[] SmartRent(int size) =>
			size < Threshold ? new byte[size] : Pool.Rent(size);

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private void SmartReturn(byte[] array)
		{
			if (array.Length >= Threshold) 
				Pool.Return(array);
		}

		[Benchmark]
		public unsafe void AllocHGlobal()
		{
			var array = Marshal.AllocHGlobal(Size);
			UseArray((byte*)array.ToPointer());
			Marshal.FreeHGlobal(array);
		}

		[Benchmark]
		public unsafe void AllocHGlobalFinally()
		{
			var array = Marshal.AllocHGlobal(Size);
			try
			{
				UseArray((byte*)array.ToPointer());
			}
			finally
			{
				Marshal.FreeHGlobal(array);
			}
		}

		private unsafe void UseArray(byte* p)
		{
			*p = 0xAA;
			*(p + Size - 1) = 0x55;
		}
	}
	/*
	BenchmarkDotNet=v0.13.1, OS=Windows 10.0.19044.1586 (21H2)
	AMD Ryzen 5 3600, 1 CPU, 12 logical and 6 physical cores
	.NET SDK=6.0.201
	  [Host]     : .NET 5.0.15 (5.0.1522.11506), X64 RyuJIT
	  DefaultJob : .NET 5.0.15 (5.0.1522.11506), X64 RyuJIT

	|                Method |  Size |         Mean |     Error |    StdDev |  Gen 0 | Allocated |
	|---------------------- |------ |-------------:|----------:|----------:|-------:|----------:|
	|          NewByteArray |    32 |     4.659 ns | 0.0319 ns | 0.0266 ns | 0.0067 |      56 B |
	|        PoolRentReturn |    32 |    22.006 ns | 0.4623 ns | 0.6777 ns |      - |         - |
	| PoolRentReturnFinally |    32 |    23.569 ns | 0.3503 ns | 0.3277 ns |      - |         - |
	|          AllocHGlobal |    32 |    74.074 ns | 0.8751 ns | 0.8185 ns |      - |         - |
	|   AllocHGlobalFinally |    32 |    77.727 ns | 1.4962 ns | 1.4694 ns |      - |         - |
	|          NewByteArray |  1024 |    30.218 ns | 0.6332 ns | 0.5923 ns | 0.1253 |   1,048 B |
	|        PoolRentReturn |  1024 |    21.499 ns | 0.0950 ns | 0.0888 ns |      - |         - |
	| PoolRentReturnFinally |  1024 |    23.395 ns | 0.1559 ns | 0.1382 ns |      - |         - |
	|          AllocHGlobal |  1024 |    74.174 ns | 1.3297 ns | 1.1787 ns |      - |         - |
	|   AllocHGlobalFinally |  1024 |    75.901 ns | 0.1468 ns | 0.1146 ns |      - |         - |
	|          NewByteArray | 65535 | 1,260.709 ns | 1.4556 ns | 1.1365 ns | 7.8106 |  65,560 B |
	|        PoolRentReturn | 65535 |    21.500 ns | 0.0299 ns | 0.0265 ns |      - |         - |
	| PoolRentReturnFinally | 65535 |    23.350 ns | 0.1060 ns | 0.0991 ns |      - |         - |
	|          AllocHGlobal | 65535 |   142.286 ns | 2.2879 ns | 1.7862 ns |      - |         - |
	|   AllocHGlobalFinally | 65535 |   151.852 ns | 3.0494 ns | 3.2628 ns |      - |         - |
    */
}
