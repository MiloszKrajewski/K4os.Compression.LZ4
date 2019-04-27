using System;
using System.IO;
using K4os.Compression.LZ4.Internal;
using K4os.Compression.LZ4.Legacy.Test.Internal;
using Xunit;

namespace K4os.Compression.LZ4.Legacy.Test
{
	public class DecoderTests
	{
#if DEBUG
		[Theory(Skip = "Too long")]
#else
		[Theory]
		#endif
		[InlineData(false, Mem.M1, Mem.K8)]
		[InlineData(false, Mem.K8, Mem.M1)]
		[InlineData(true, Mem.M1, Mem.K8)]
		[InlineData(true, Mem.K8, Mem.M1)]
		[InlineData(false, Mem.M1, Mem.K8 + 1337)]
		[InlineData(false, Mem.K8, Mem.M1 + 1337)]
		public void WholeCorpus(bool high, int block, int chunk)
		{
			foreach (var filename in Tools.CorpusNames)
			{
				try
				{
					TestDecoder($".corpus/{filename}", high, block, chunk);
				}
				catch (Exception e)
				{
					throw new Exception($"Failed to process: {filename}", e);
				}
			}
		}

		[Theory]
		[InlineData("reymont", false, Mem.M1)]
		[InlineData("reymont", true, Mem.K256)]
		public void DecoderStillWorksWhenReadingSlowFile(string original, bool high, int block)
		{
			original = Tools.FindFile($".corpus/{original}");

			var encoded = Path.GetTempFileName();
			var decoded = Path.GetTempFileName();
			try
			{
				TestedLZ4.Encode(original, encoded, high, block, Mem.K64);
				
				using (var source = LZ4Legacy.Decode(new FakeNetworkStream(File.OpenRead(encoded))))
				using (var target = File.Create(decoded))
				{
					var buffer = new byte[Mem.K64];
					while (true)
					{
						var read = source.Read(buffer, 0, buffer.Length);
						if (read == 0)
							break;

						target.Write(buffer, 0, read);
					}
				}
				
				Tools.SameFiles(original, decoded);
			}
			finally
			{
				File.Delete(encoded);
				File.Delete(decoded);
			}
		}

		private static void TestDecoder(string original, bool high, int block, int chunk)
		{
			original = Tools.FindFile(original);
			var encoded = Path.GetTempFileName();
			var decoded = Path.GetTempFileName();
			try
			{
				ReferenceLZ4.Encode(original, encoded, high, block, chunk);
				TestedLZ4.Decode(encoded, decoded, chunk);
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
