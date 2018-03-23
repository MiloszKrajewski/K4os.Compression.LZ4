using System.IO;
using Xunit;

namespace K4os.Compression.LZ4.Streams.Test
{
	public class RoundtripTests
	{
		[Theory]
		[InlineData(".corpus/dickens", "-9 -BD -B4 -BX")]
		public void RoundtripWithReferenceEncoder(string filename, string options)
		{
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
			#warning implement me
			//using (var input = File.OpenRead(encoded))
			//using (var output = File.Create(decoded))
			//using (var decode = new LZ4InputStream(input))
			//{
			//	var buffer = new byte[4096];
			//	while (true)
			//	{
			//		var read = decode.Read(buffer, 0, buffer.Length);
			//		if (read == 0)
			//			break;

			//		output.Write(buffer, 0, read);
			//	}
			//}
		}
	}
}
