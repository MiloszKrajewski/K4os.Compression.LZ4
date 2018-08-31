using System.IO;
using K4os.Compression.LZ4.Streams.Test.Internal;
using Xunit;

namespace K4os.Compression.LZ4.Streams.Test
{
	public class RoundtripTests
	{
		[Theory]
		[InlineData("dickens", LZ4Level.L00_FAST, Mem.K64)]
		[InlineData("mozilla", LZ4Level.L00_FAST, Mem.K64)]
		[InlineData("mr", LZ4Level.L00_FAST, Mem.K64)]
		[InlineData("nci", LZ4Level.L00_FAST, Mem.K64)]
		[InlineData("ooffice", LZ4Level.L00_FAST, Mem.K64)]
		[InlineData("osdb", LZ4Level.L00_FAST, Mem.K64)]
		[InlineData("reymont", LZ4Level.L00_FAST, Mem.K64)]
		[InlineData("samba", LZ4Level.L00_FAST, Mem.K64)]
		[InlineData("sao", LZ4Level.L00_FAST, Mem.K64)]
		[InlineData("webster", LZ4Level.L00_FAST, Mem.K64)]
		[InlineData("x-ray", LZ4Level.L00_FAST, Mem.K64)]
		[InlineData("xml", LZ4Level.L00_FAST, Mem.K64)]
		public void SmallBlockSize(string fileName, LZ4Level level, int blockSize)
		{
			TestEncoder(
				$".corpus/{fileName}", 1000,
				new LZ4Settings { Level = level, BlockSize = blockSize });
		}

		private static void TestEncoder(string fileName, int chunkSize, LZ4Settings settings)
		{
			var original = Tools.FindFile(fileName);
			var encoded = Path.GetTempFileName();
			var decoded = Path.GetTempFileName();

			try
			{
				TestedLZ4.Encode(original, encoded, chunkSize, settings);
				TestedLZ4.Decode(encoded, decoded);
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
