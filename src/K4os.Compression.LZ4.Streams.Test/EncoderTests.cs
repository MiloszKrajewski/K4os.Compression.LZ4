using System;
using System.IO;
using K4os.Compression.LZ4.Internal;
using K4os.Compression.LZ4.Streams.Test.Internal;
using Xunit;

namespace K4os.Compression.LZ4.Streams.Test
{
	public class EncoderTests
	{
		[Theory]
		//[InlineData("reymont", "-1 -BD -B4", 1337)]
		//[InlineData("reymont", "-9 -BD -B7", 1337)]
		[InlineData("x-ray", "-1 -BD -B4", 1337)]
		//[InlineData("x-ray", "-9 -BD -B7", 1337)]
		public void OddChunkSize(string filename, string options, int chunkSize)
		{
			TestEncoder($".corpus/{filename}", chunkSize, Tools.ParseSettings(options));
		}

		[Theory]
		[InlineData("reymont", "-1 -BD -B4", Mem.M1)]
		[InlineData("x-ray", "-9 -BD -B4", Mem.M1)]
		public void LargeChunkSize(string filename, string options, int chunkSize)
		{
			TestEncoder($".corpus/{filename}", chunkSize, Tools.ParseSettings(options));
		}

		[Theory]
		[InlineData("-1 -BD -B4 -BX", Mem.K64)]
		[InlineData("-1 -BD -B4 -BX", 1337)]
		[InlineData("-1 -BD -B4 -BX", Mem.K64 + 1337)]
		[InlineData("-9 -BD -B7", Mem.K64)]
		[InlineData("-9 -BD -B7", 1337)]
		[InlineData("-9 -BD -B7", Mem.K64 + 1337)]
		public void HighEntropyData(string options, int chunkSize)
		{
			var filename = Path.GetTempFileName();
			try
			{
				Tools.WriteRandom(filename, 10 * Mem.M1 + 1337);
				TestEncoder(filename, chunkSize, Tools.ParseSettings(options));
			}
			finally
			{
				File.Delete(filename);
			}
		}

		#if DEBUG
		[Theory(Skip = "Too long")]
		#else
		[Theory]
		#endif
		[InlineData("-1 -BD -B4 -BX", Mem.K8)]
		[InlineData("-1 -BD -B5", Mem.K8)]
		[InlineData("-1 -BD -B6 -BX", Mem.K8)]
		[InlineData("-1 -BD -B7", Mem.K4)]
		[InlineData("-9 -BD -B4", Mem.K4)]
		[InlineData("-9 -BD -B5 -BX", Mem.K4)]
		[InlineData("-9 -BD -B6", Mem.K4)]
		[InlineData("-9 -BD -B7 -BX", Mem.K4)]
		[InlineData("-1 -B4", Mem.K4)]
		[InlineData("-1 -B7", Mem.K4)]
		[InlineData("-9 -B7 -BX", Mem.K4)]
		[InlineData("-1 -B4 -BD", Mem.M1)]
		[InlineData("-9 -B4 -BD", 1337)]
		public void WholeCorpus(string options, int chunkSize)
		{
			var settings = Tools.ParseSettings(options);
			foreach (var filename in Tools.CorpusNames)
			{
				try
				{
					TestEncoder($".corpus/{filename}", chunkSize, settings);
				}
				catch (Exception e)
				{
					throw new Exception(
						$"Failed to process: {filename} @ {options}/{chunkSize}", e);
				}
			}
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
