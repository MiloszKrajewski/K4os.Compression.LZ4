using System;
using System.Buffers;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using BenchmarkDotNet.Attributes;
using K4os.Compression.LZ4.Internal;
using Mem = K4os.Compression.LZ4.vPrev.Internal.Mem;

namespace K4os.Compression.LZ4.Benchmarks
{
	public class FixedMemoryAllocation
	{
		// [Params(64, Mem.K1, Mem.K64, Mem.K512, Mem.M1, Mem.M4)]
		[Params(128, Mem.K1, Mem.K64)]
		public int Size { get; set; }

		[Params(false, true)]
		public bool Clear { get; set; }

		[Benchmark]
		public unsafe void UseAllocHGlobal()
		{
			byte* ptr = null;
			try
			{
				ptr = (byte*)(Clear ? Mem.AllocZero(Size) : Mem.Alloc(Size));
			}
			finally
			{
				Mem.Free(ptr);
			}
		}

		[Benchmark]
		public unsafe void UseSharedPoolAndPinning()
		{
			byte* ptr;
			byte[] arr = null;
			GCHandle hndl = default;
			try
			{
				arr = ArrayPool<byte>.Shared.Rent(Size);
				hndl = GCHandle.Alloc(arr, GCHandleType.Pinned);
				ptr = (byte*)hndl.AddrOfPinnedObject().ToPointer();
				if (Clear) Unsafe.InitBlockUnaligned(ptr, 0, (uint)Size);
				Noop((IntPtr)ptr);
			}
			finally
			{
				hndl.Free();
				if (arr != null) ArrayPool<byte>.Shared.Return(arr);
			}
		}
		
		[Benchmark]
		public unsafe void UseManagedAndPinning()
		{
			byte* ptr;
			byte[] arr = null;
			GCHandle hndl = default;
			try
			{
				arr = new byte[Size];
				hndl = GCHandle.Alloc(arr, GCHandleType.Pinned);
				ptr = (byte*)hndl.AddrOfPinnedObject().ToPointer();
				Noop((IntPtr)ptr);
			}
			finally
			{
				hndl.Free();
			}
		}


		[Benchmark]
		public unsafe void UsePinnedMemory()
		{
			PinnedMemory.Alloc(out var pin, Size, Clear);
			try
			{
				var ptr = pin.Pointer;
				Noop((IntPtr)ptr);
			}
			finally
			{
				pin.Free();
			}
		}

		[MethodImpl(MethodImplOptions.NoInlining)]
		// ReSharper disable once UnusedParameter.Local
		private static void Noop<T>(T _) { }

		[Benchmark]
		public void UsePinnedHeap()
		{
			byte[] arr = null;
			try
			{
				arr = Clear
					? GC.AllocateArray<byte>(Size, true)
					: GC.AllocateUninitializedArray<byte>(Size, true);
				Noop(arr);
			}
			finally
			{
				Noop(arr);
			}
		}
	}
}
