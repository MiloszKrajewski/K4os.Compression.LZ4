using System;
using System.IO;
using K4os.Compression.LZ4.Encoders;
using Xunit;

namespace K4os.Compression.LZ4.Streams.Test
{
	public class RoundtripTests
	{
		private static readonly string[] CorpusNames = new string[] {
			"dickens", // x/1384
			"mozilla", // 2417/913
			"mr", // 1088/3675
			"nci", // 1536/4049
			"ooffice", // x/3778
			"osdb", // x/1311
			"reymont", // 3348/1607
			"samba", // 69/3098
			"sao",
			"webster", // 817/2183
			"x-ray", // x/365
			"xml" // 123/1581
		};

		[Theory]
		[InlineData("reymont", "-1 -BD -B4")]
		[InlineData("reymont", "-9 -BD -B4")]
		[InlineData("xml", "-1 -BD -B4")]
		[InlineData("xml", "-9 -BD -B4")]
		public void RoundtripWithReferenceEncoderSmallBlockSize(string filename, string options)
		{
			TestDecoder(Path.Combine(".corpus", filename), options);
		}

#if DEBUG
		[Theory(Skip = "Too long")]
#else
		[Theory]
#endif
		[InlineData("-1 -BD -B4 -BX")]
		[InlineData("-1 -BD -B5")]
		[InlineData("-1 -BD -B6 -BX")]
		[InlineData("-1 -BD -B7")]
		[InlineData("-9 -BD -B4")]
		[InlineData("-9 -BD -B5 -BX")]
		[InlineData("-9 -BD -B6")]
		[InlineData("-9 -BD -B7 -BX")]
		public void RoundtripWithReferenceEncoderForWholeCorpus(string options)
		{
			foreach (var filename in CorpusNames)
			{
				try
				{
					TestDecoder(Path.Combine(".corpus", filename), options);
				}
				catch (Exception e)
				{
					throw new Exception($"Failed to process: {filename} @ {options}", e);
				}
			}
		}

		private static void TestDecoder(string filename, string options)
		{
			var original = Tools.FindFile(filename);
			var encoded = ReferenceLZ4.Encode(options, original);
			var decoded = Path.GetTempFileName();
			try
			{
				Decode(encoded, decoded);
				Tools.SameFiles(original, decoded);
			}
			finally
			{
				File.Delete(encoded);
				File.Delete(decoded);
			}
		}

		private static void Decode(string encoded, string decoded)
		{
			using (var input = File.OpenRead(encoded))
			using (var output = File.Create(decoded))
			using (var decode = new LZ4InputStream(input, i => new LZ4StreamDecoder(i.BlockSize, 0)))
			{
				var buffer = new byte[4096];
				while (true)
				{
					var read = decode.Read(buffer, 0, buffer.Length);
					if (read == 0)
						break;

					output.Write(buffer, 0, read);
				}
			}
		}
	}
}
