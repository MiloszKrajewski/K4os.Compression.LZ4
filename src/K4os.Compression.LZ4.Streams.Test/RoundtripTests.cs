using System;
using System.IO;
using K4os.Compression.LZ4.Internal;
using K4os.Compression.LZ4.Streams.Test.Internal;
using TestHelpers;
using Xunit;

namespace K4os.Compression.LZ4.Streams.Test;

public class RoundtripTests
{
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
		var settings = Settings.ParseSettings(options);
		foreach (var filename in Tools.CorpusNames)
		{
			try
			{
				TestRoundtrip($".corpus/{filename}", chunkSize, settings);
			}
			catch (Exception e)
			{
				throw new Exception(
					$"Failed to process: {filename} @ {options}/{chunkSize}", e);
			}
		}
	}

	[Theory]
	[InlineData("reymont", "-1 -B4")]
	[InlineData("mozilla", "-9 -B5")]
	[InlineData("x-ray", "-12 -B7")]
	public void SelectiveRoundtrip(string filename, string options)
	{
		var settings = Settings.ParseSettings(options);
		TestRoundtrip($".corpus/{filename}", 1337, settings);
	}

	private static void TestRoundtrip(string fileName, int chunkSize, LZ4Settings settings)
	{
		var original = Tools.FindFile(fileName);
		var encoded = Path.GetTempFileName();
		var decoded = Path.GetTempFileName();

		try
		{
			TestedLZ4.Encode(original, encoded, chunkSize, settings);
			TestedLZ4.Decode(encoded, decoded, chunkSize);
			Tools.SameFiles(original, decoded);
		}
		finally
		{
			File.Delete(encoded);
			File.Delete(decoded);
		}
	}
}