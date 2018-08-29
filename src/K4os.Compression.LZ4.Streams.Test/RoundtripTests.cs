using System.IO;
using Xunit;

namespace K4os.Compression.LZ4.Streams.Test
{
	public class RoundtripTests
	{
		[Theory]
		//[InlineData("dickens", false, Mem.K64)]
		//[InlineData("mozilla", false, Mem.K64)]
		//[InlineData("mr", false, Mem.K64)]
		//[InlineData("nci", false, Mem.K64)]
		//[InlineData("ooffice", false, Mem.K64)]
		//[InlineData("osdb", false, Mem.K64)]
		//[InlineData("reymont", false, Mem.K64)]
		//[InlineData("samba", false, Mem.K64)]
		//[InlineData("sao", false, Mem.K64)]
		//[InlineData("webster", false, Mem.K64)]
		[InlineData("x-ray", false, Mem.K64)]
		//[InlineData("xml", false, Mem.K64)]
		public void SmallBlockSize(string filename, bool compressionLevel, int blockSize)
		{
			var tempFile = ExtractChunkOf(filename, Mem.K64);
			var frameInfo = new LZ4FrameInfo(false, true, false, null, blockSize);
			TestEncoder(tempFile, frameInfo);
		}

		private static void TestEncoder(string filename, ILZ4FrameInfo frameInfo)
		{
			var original = Tools.FindFile(filename);
			var encoded = Path.GetTempFileName();
			var decoded = Path.GetTempFileName();

			try
			{
				TestedLZ4.Encode(original, encoded, frameInfo);
				TestedLZ4.Decode(encoded, decoded);
				Tools.SameFiles(original, decoded);
			}
			finally
			{
				File.Delete(encoded);
				File.Delete(decoded);
			}
		}

		private static string ExtractChunkOf(string filename, int length)
		{
			var tempFile = Path.GetTempFileName();
			using (var source = File.OpenRead(Tools.FindFile(Path.Combine(".corpus", filename))))
			using (var target = File.Create(tempFile))
			{
				var buffer = new byte[length];
				source.Read(buffer, 0, buffer.Length);
				target.Write(buffer, 0, buffer.Length);
			}

			return tempFile;
		}
	}
}
