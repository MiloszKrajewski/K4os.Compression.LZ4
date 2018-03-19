using System.IO;

namespace K4os.Compression.LZ4.Streams.Test
{
	public class UnitTest1
	{
		// ..\.tools\lz4.exe -9 -BD -B4 -BX -f .\dickens .\dickens.lz4
		public void TestDecoder()
		{
			var original = Tools.FindFile(".corpus/dickens");
			var encoded = ReferenceLZ4.Encode("-9 -BD -B4 -BX", original);
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
			using (var decode = new LZ4InputStream(input))
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
