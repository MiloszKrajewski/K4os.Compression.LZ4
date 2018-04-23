using System;
using System.IO;
using K4os.Compression.LZ4.Encoders;
using Xunit;

namespace K4os.Compression.LZ4.Streams.Test
{
	public class ReferenceEncoderRoundtripTests
	{
		private static readonly string[] CorpusNames = {
			"dickens", "mozilla", "mr", "nci",
			"ooffice", "osdb", "reymont", "samba",
			"sao", "webster", "x-ray", "xml"
		};

		[Theory]
		[InlineData("reymont", "-1 -BD -B4")]
		[InlineData("reymont", "-9 -BD -B4")]
		[InlineData("xml", "-1 -BD -B4")]
		[InlineData("xml", "-9 -BD -B4")]
		public void SmallBlockSize(string filename, string options)
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
		[InlineData("-1 -B4")]
		[InlineData("-1 -B7")]
		[InlineData("-9 -B7 -BX")]
		public void WholeCorpus(string options)
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

		[Theory]
		[InlineData("-1 -BD -B4 -BX")]
		public void HighEntropyData(string options)
		{
			var random = new Random(0);
			var buffer = new byte[0x10000];
			var filename = Path.GetTempFileName();
			using (var file = File.Create(filename))
			{
				for (var i = 0; i < Mem.M4 / Mem.K64; i++)
				{
					random.NextBytes(buffer);
					file.Write(buffer, 0, buffer.Length);
				}
			}

			TestDecoder(filename, options);
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
