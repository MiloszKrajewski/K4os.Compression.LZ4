using System;
using K4os.Compression.LZ4.Encoders;
using Xunit;

namespace K4os.Compression.LZ4.Test
{
	public class HighEntropyRepeatedTests
	{
		[Fact]
		public void t1()
		{
			var random = new Random(0);
			var encoder = new LZ4FastEncoder(256, 0);
			var source = new byte[256];
			random.NextBytes(source);
			var target = new byte[1024];
			Assert.Equal(256, encoder.Topup(source, 0, 256));
			Assert.Equal(258, encoder.Encode(target, 0, 1024));

			encoder.Topup(source, 240, 16);
			var xxx = encoder.Encode(target, 0, 1024);
		}
	}
}
