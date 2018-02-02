using System;
using System.Linq;
using System.Text;
using LZ4;
using Xunit;

namespace K4os.Compression.LZ4.Test
{
	public class UnitTest1
	{
		const string Lorem =
			"Lorem ipsum dolor sit amet, consectetur adipiscing elit, " +
			"sed do eiusmod tempor incididunt ut labore et dolore magna aliqua. " +
			"Ut enim ad minim veniam, quis nostrud exercitation ullamco laboris " +
			"nisi ut aliquip ex ea commodo consequat. Duis aute irure dolor in " +
			"reprehenderit in voluptate velit esse cillum dolore eu fugiat nulla pariatur. " +
			"Excepteur sint occaecat cupidatat non proident, " +
			"sunt in culpa qui officia deserunt mollit anim id est laborum.";
		const string Hello = "h";

		[Fact]
		public void Test1()
		{
			var template = "\u0000\u0001\u0002\u0003\u0004\u0005\u0006\u0007\u0008\u0009";
			var source = Encoding.UTF8.GetBytes(Enumerable.Repeat(template, 3).Aggregate((a, b) => a + b));
			var target1 = LZ4Codec.Encode(source, 0, source.Length);
			var target2 = new byte[LZ4Interface.MaximumOutputSize(source.Length)];
			LZ4Interface.Encode(source, target2);
			var diff = Compare(target1, target2, target1.Length);
			var compressedLength = LZ4Interface.Encode(source, target2);
			var result = new byte[source.Length];
			var decompressedLength = LZ4Interface.Decode(target2, result, compressedLength);

			for (int i = 0; i < source.Length; i++)
				if (source[i] != result[i])
					throw new Exception($"Difference found @ {i}/{source.Length}");
		}

		private static int Compare(byte[] source, byte[] target, int length)
		{
			for (var i = 0; i < length; i++)
			{
				if (source[i] != target[i])
					return i;
			}
			return -1;
		}
	}
}
