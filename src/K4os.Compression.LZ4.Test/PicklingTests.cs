using System;
using K4os.Compression.LZ4.Internal;
using Xunit;

namespace K4os.Compression.LZ4.Test
{
	public class PicklingTests
	{
		[Theory]
		[InlineData(10)]
		[InlineData(32)]
		[InlineData(1337)]
		[InlineData(1337, LZ4Level.L09_HC)]
		[InlineData(0x10000)]
		[InlineData(0x172a5, LZ4Level.L00_FAST)]
		[InlineData(0x172a5, LZ4Level.L09_HC)]
		[InlineData(0x172a5, LZ4Level.L11_OPT)]
		[InlineData(0x172a5, LZ4Level.L12_MAX)]
		public void PickleLorem(int length, LZ4Level level = LZ4Level.L00_FAST)
		{
			var original = new byte[length];
			Lorem.Fill(original, 0, length);
			
			var pickled = LZ4Codec.Pickle(original, level);
			var unpickled = LZ4Codec.Unpickle(pickled);
			
			Tools.SameBytes(original, unpickled);
		}
		
		[Theory]
		[InlineData(1, 15)]
		[InlineData(2, 1024)]
		[InlineData(3, 1337, LZ4Level.L09_HC)]
		[InlineData(3, 1337, LZ4Level.L12_MAX)]
		[InlineData(4, Mem.K64, LZ4Level.L12_MAX)]
		[InlineData(5, Mem.M4, LZ4Level.L12_MAX)]
		public void PickleEntropy(int seed, int length, LZ4Level level = LZ4Level.L00_FAST)
		{
			var original = new byte[length];
			new Random(seed).NextBytes(original);
			
			var pickled = LZ4Codec.Pickle(original, level);
			var unpickled = LZ4Codec.Unpickle(pickled);
			
			Tools.SameBytes(original, unpickled);
		}

	}
}
