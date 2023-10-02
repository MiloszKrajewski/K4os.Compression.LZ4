using System;
using System.Runtime.InteropServices;
using Xunit;

namespace K4os.Compression.LZ4.Tests;

// NOTE: you should not test framework, but I needed to verify how it works
public class FrameworkTests
{
	[Theory]
	[InlineData(100, 1024*1024*10)]
	public unsafe void AllocHGlobalZeroesMemory(int rounds, int size)
	{
		// This is not a real test
		// I was just trying to check if AllocHGlobal zeroes memory
		// documentation says it does not, but it seems like it does
		// but to be on safer side - all code assumes it doesn't
		for (var r = 0; r < rounds; r++)
		{
			var ptr = Marshal.AllocHGlobal(size * sizeof(int));
			
			Assert.True(ptr.ToPointer() != null);
			var span = new Span<int>(ptr.ToPointer(), size);
			for (var i = 0; i < size; i++)
			{
				if (span[i] != 0) throw new Exception("Not zeroed");
				span[i] = 0x5A6B7C8D;
			}
		
			Marshal.FreeHGlobal(ptr);
		}
	}

	[Fact]
	public void PinnedArrayIsNotCollected()
	{
		var array1 = new byte[1024*1024*10];
		var array2 = new byte[1024*1024*10];
		var weak1 = new WeakReference(array1);
		var weak2 = new WeakReference(array2);

		var handle1 = GCHandle.Alloc(array1, GCHandleType.Pinned);
		array1 = null;
		array2 = null;

		try
		{
			for (var i = 0; i < 100; i++)
			{
				for (var j = 0; j < 1000; j++) _ = new byte[1024 * 1024];
				GC.Collect();
				GC.WaitForPendingFinalizers();
				GC.Collect();
			}
			
			// it should collect array2 (only weak reference),
			// but not array1 (because it is pinned)
			Assert.True(weak1.IsAlive);
			Assert.False(weak2.IsAlive);
		}
		finally
		{
			handle1.Free();
		}
	}
}
