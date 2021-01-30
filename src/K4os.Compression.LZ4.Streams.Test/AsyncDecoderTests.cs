using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using K4os.Compression.LZ4.Streams.Test.Internal;
using TestHelpers;
using Xunit;

namespace K4os.Compression.LZ4.Streams.Test
{
	public class AsyncDecoderTests
	{
		[Theory]
		[InlineData(".corpus/dickens", "-1 -BD -B4 -BX")]
		[InlineData(".corpus/mozilla", "-1 -BD -B7")]
		public async Task TestDecoder(string original, string options)
		{
			original = Tools.FindFile(original);
			var compressed = Path.GetTempFileName();
			var decompressed = Path.GetTempFileName();
			try
			{
				ReferenceLZ4.Encode(options, original, compressed);

				await Task.CompletedTask;
				
				using (var src = LZ4Stream.Decode(File.OpenRead(compressed)))
				using (var dst = File.Create(decompressed))
				{
					await src.CopyToAsync(dst);
				}
				
				Tools.SameFiles(original, decompressed);
			}
			finally
			{
				File.Delete(compressed);
				File.Delete(decompressed);
			}
		}
	}
}
