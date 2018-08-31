using System.IO;
using K4os.Compression.LZ4.Streams.Test.Internal;
using Xunit;

namespace K4os.Compression.LZ4.Streams.Test
{
	public class EncoderTests
	{
		private static readonly string[] CorpusNames = {
			"dickens", "mozilla", "mr", "nci",
			"ooffice", "osdb", "reymont", "samba",
			"sao", "webster", "x-ray", "xml"
		};

		[Theory]
		[InlineData("reymont", Mem.K64)]
		[InlineData("xml", Mem.K64)]
		public void SmallBlockSize(string filename, int blockSize)
		{
			var frameInfo = new LZ4FrameInfo(false, true, false, null, blockSize);
			TestEncoder(Path.Combine(".corpus", filename), frameInfo);
		}

		private static void TestEncoder(string filename, ILZ4FrameInfo frameInfo)
		{
			var original = Tools.FindFile(filename);
			var encoded = Path.GetTempFileName();
			string decoded = Path.GetTempFileName();
			try
			{
				TestedLZ4.Encode(original, encoded, frameInfo);
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