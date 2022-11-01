using System;
using System.Runtime.InteropServices;
using Xunit;

namespace K4os.Compression.LZ4.Test;

// NOTE: you should not test framework, but I needed to verify how it works
public class FrameworkTests
{
	[Theory]
	[InlineData(100, 1024*1024*10)]
	public unsafe void Test1(int rounds, int size)
	{
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
}
