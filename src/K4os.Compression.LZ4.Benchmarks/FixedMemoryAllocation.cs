using System;
using System.Buffers;
using System.Runtime.InteropServices;
using BenchmarkDotNet.Attributes;

namespace K4os.Compression.LZ4.Benchmarks
{
	public class FixedMemoryAllocation
	{
		[Params(64, 128, 1024)]
		public int SizeK { get; set; }

		private int SizeB => SizeK * 1024 + 32;

		[Benchmark]
		public unsafe void UseAllocHGlobal()
		{
			byte* ptr = null;
			try
			{
				ptr = (byte*)Marshal.AllocHGlobal(SizeB).ToPointer();
			}
			finally
			{
				if (ptr != null) Marshal.FreeHGlobal(new IntPtr(ptr));
			}
		}

		[Benchmark]
		public unsafe void UseSharedPoolAndPinning()
		{
			byte* ptr = null;
			byte[] arr = null;
			GCHandle hndl = default;
			try
			{
				arr = ArrayPool<byte>.Shared.Rent(SizeB);
				hndl = GCHandle.Alloc(arr, GCHandleType.Pinned);
				ptr = (byte*)hndl.AddrOfPinnedObject().ToPointer();
			}
			finally
			{
				ptr = null;
				hndl.Free();
				if (arr != null) ArrayPool<byte>.Shared.Return(arr);
			}
		}
	}
}
