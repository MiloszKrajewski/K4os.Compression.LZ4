using System;
using System.Diagnostics.CodeAnalysis;
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
		[SuppressMessage("ReSharper", "UseAwaitUsing")]
		public async Task UseDecoderWithCopyAsync(string original, string options)
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

		[Theory]
		[InlineData(".corpus/dickens", null, 137)]
		[InlineData(".corpus/dickens", null, 1337)]
		[InlineData(".corpus/mozilla", null, 1337*1337)]
		public async Task AsyncReaderReadExactlyTheSameDataAsSyncOne(
			string original, string options, int chunkSize)
		{
			original = Tools.FindFile(original);
			options ??= "-1 -BD -B4 -BX";
			var compressed = Path.GetTempFileName();
			var decompressed = Path.GetTempFileName();
			
			try
			{
				ReferenceLZ4.Encode(options, original, compressed);
				var buffer = new byte[chunkSize];

				await Task.CompletedTask;
				
				using (var src = LZ4Stream.Decode(File.OpenRead(compressed)))
				using (var dst = File.Create(decompressed))
				{
					while (true)
					{
						var bytes = await src.ReadAsync(buffer, 0, chunkSize);
						if (bytes == 0) break;

						await dst.WriteAsync(buffer, 0, bytes);
					}
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
