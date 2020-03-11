using System;
using System.IO;
using K4os.Compression.LZ4.Internal;
using K4os.Compression.LZ4.Legacy.Test.Internal;
using TestHelpers;
using Xunit;

namespace K4os.Compression.LZ4.Legacy.Test
{
	public class EncoderTests
	{
		[Theory]
		[InlineData(false, Mem.M1, Mem.K8)]
		[InlineData(false, Mem.K8, Mem.M1)]
		[InlineData(true, Mem.K8, Mem.M1)]
		[InlineData(true, Mem.M1, Mem.K8)]
		[InlineData(false, Mem.M1, Mem.K8 + 1337)]
		[InlineData(false, Mem.K8, Mem.M1 + 1337)]
		public void WholeCorpus(bool high, int block, int chunk)
		{
			foreach (var filename in Tools.CorpusNames)
			{
				try
				{
					TestEncoder($".corpus/{filename}", high, block, chunk);
				}
				catch (Exception e)
				{
					throw new Exception($"Failed to process: {filename}", e);
				}
			}
		}

		private static void TestEncoder(string original, bool high, int block, int chunk)
		{
			original = Tools.FindFile(original);
			var encoded = Path.GetTempFileName();
			var decoded = Path.GetTempFileName();
			try
			{
				TestedLZ4.Encode(original, encoded, high, block, chunk);
				ReferenceLZ4.Decode(encoded, decoded, chunk);
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
