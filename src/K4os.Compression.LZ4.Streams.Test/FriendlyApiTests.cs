using System.IO;
using System.Runtime.InteropServices;
using K4os.Compression.LZ4.Streams.Test.Internal;
using Xunit;

namespace K4os.Compression.LZ4.Streams.Test
{
	public class FriendlyApiTests
	{
		[Fact]
		public void WholeCorpus()
		{
			foreach (var filename in Tools.CorpusNames)
			{
				TestRoundtrip($".corpus/{filename}");
			}
		}

		private static void TestRoundtrip(string filename)
		{
			var original = Tools.FindFile(filename);
			var encoded = Path.GetTempFileName();
			var decoded = Path.GetTempFileName();
			try
			{
				Encode(original, encoded);
				Decode(encoded, decoded);
				Tools.SameFiles(original, decoded);
			}
			finally
			{
				File.Delete(encoded);
				File.Delete(decoded);
			}
		}

		private static void Encode(string original, string encoded)
		{
			using (var input = File.OpenRead(original))
			using (var output = LZ4Stream.Encode(File.Create(encoded)))
				input.CopyTo(output);
		}
		
		private static void Decode(string encoded, string decoded)
		{
			using (var input = LZ4Stream.Decode(File.OpenRead(encoded)))
			using (var output = File.Create(decoded))
				input.CopyTo(output);
		}
	}
}
