using System;
using System.IO;
using System.Threading.Tasks;
using TestHelpers;
using Xunit;

namespace K4os.Compression.LZ4.Streams.Test
{
	public class AsyncEncoderTests
	{
		[Theory]
		[InlineData(".corpus/dickens", 0)]
		[InlineData(".corpus/dickens", 1337)]
		[InlineData(".corpus/mozilla", 1337*1337)]
		public async Task AsyncStreamProduceBinaryIdenticalOutput(string filename, int seed)
		{
			filename = Tools.FindFile(filename);
			var source = File.ReadAllBytes(filename);

			using (var memoryA = new MemoryStream())
			using (var memoryB = new MemoryStream())
			{
				using (var streamA = LZ4Stream.Encode(memoryA, LZ4Level.L00_FAST, leaveOpen: true))
				using (var streamB = LZ4Stream.Encode(memoryB, LZ4Level.L00_FAST, leaveOpen: true))
				{
					var offset = 0;
					var random = new Random(seed);

					while (true)
					{
						var chunk = Math.Min(
							(int) (random.NextExp(10)*1024*1024) + 1,
							source.Length - offset);

						if (chunk == 0) break;

						streamA.Write(source, offset, chunk);
						await streamB.WriteAsync(source, offset, chunk);

						offset += chunk;
					}
				}

				var bytesA = memoryA.ToArray();
				var bytesB = memoryB.ToArray();
				
				Tools.SameBytes(bytesA, bytesB);
			}
		}
	}
}
