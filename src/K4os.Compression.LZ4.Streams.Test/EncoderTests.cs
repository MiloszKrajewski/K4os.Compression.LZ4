using System.IO;
using K4os.Compression.LZ4.Streams.Test.Internal;
using Xunit;

namespace K4os.Compression.LZ4.Streams.Test
{
	public class EncoderTests
	{
		[Theory]
		[InlineData("reymont", Mem.K64)]
		[InlineData("xml", Mem.K64)]
		public void SmallBlockSize(string filename, int blockSize)
		{
			TestEncoder($".corpus/{filename}", 1000, new LZ4Settings { BlockSize = blockSize });
		}

		private static void TestEncoder(string original, int chunkSize, LZ4Settings settings)
		{
			original = Tools.FindFile(original);
			var encoded = Path.GetTempFileName();
			var decoded = Path.GetTempFileName();
			try
			{
				TestedLZ4.Encode(original, encoded, chunkSize, settings);
				ReferenceLZ4.Decode(encoded, decoded);
				Tools.SameFiles(original, decoded);
			}
			finally
			{
				File.Delete(encoded);
				File.Delete(decoded);
			}
		}
	}
}
