using System;
using System.Text;
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

		[Fact]
		public void Test1()
		{
			var source = Encoding.UTF8.GetBytes(Lorem + Lorem + Lorem + Lorem + Lorem);
			var target = new byte[LZ4Codec.MaximumOutputSize(source.Length)];
			var length = LZ4Codec.Encode(source, target);
		}
	}
}
