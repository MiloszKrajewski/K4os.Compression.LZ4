using System;
using System.Text;
using ICSharpCode.SharpZipLib.Tar;
using K4os.Compression.LZ4.vPrev.Internal;
using TestHelpers;
using Xunit;

namespace K4os.Compression.LZ4.Streams.Tests
{
	public class TarTests
	{
		[Theory]
		[InlineData(".corpus/reymont", 0)]
		[InlineData(".corpus/reymont", 1123)]
		[InlineData(".corpus/webster", 234)]
		[InlineData(".corpus/webster", 234234)]
		public void CreateTarFile(string filename, int seed)
		{
			var sourceName = Tools.FindFile(filename);
			var tarName = TempFileName(".tar.lz4");
			var targetName = TempFileName(".data");

			try
			{
				var random = new Random(seed);

				SplitAndTar(sourceName, tarName, _ => (int) (random.NextExp() * Mem.K128));
				UntarAndMerge(tarName, targetName);

				Tools.SameFiles(sourceName, targetName);
			}
			finally
			{
				File.Delete(targetName);
				File.Delete(tarName);
			}
		}

		private static void SplitAndTar(
			string sourceName, string targetName, Func<int, int> chunkSize)
		{
			using var sourceFile = File.OpenRead(sourceName);
			using var targetFile = File.Create(targetName);
			using var lz4File = LZ4Stream.Encode(targetFile);
			using var tarFile = new TarOutputStream(lz4File, Encoding.UTF8);
			var partNumber = 0;
			
			while (true)
			{
				var partName = TempFileName(".part");
				var partSize = CopyPart(sourceFile, partName, Math.Max(1, chunkSize(partNumber)));
				if (partSize > 0)
				{
					var entry = TarEntry.CreateEntryFromFile(partName);
					entry.Name = Path.GetFileName(entry.Name);
					tarFile.PutNextEntry(entry);
					CopyPart(partName, tarFile);
					tarFile.CloseEntry();
				}
				File.Delete(partName);
				if (partSize <= 0) break;
				partNumber++;
			}
		}
		
		private static void UntarAndMerge(string sourceName, string targetName)
		{
			using var sourceFile = File.OpenRead(sourceName);
			using var targetFile = File.Create(targetName);
			using var lz4File = LZ4Stream.Decode(sourceFile);
			using var tarFile = new TarInputStream(lz4File, Encoding.UTF8);

			while (true)
			{
				var entry = tarFile.GetNextEntry();
				if (entry is null) break;
				CopyPart(tarFile, targetFile, (int) entry.Size);
			}
		}

		private static string TempFileName(string extension = null) =>
			Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}{extension ?? string.Empty}");

		private static int CopyPart(Stream sourceFile, Stream targetFile, int partSize)
		{
			var buffer = new byte[4096];
			var copied = 0;
			while (partSize > 0)
			{
				var chunkSize = Math.Min(buffer.Length, partSize);
				var read = sourceFile.Read(buffer, 0, chunkSize);
				if (read == 0) break;

				targetFile.Write(buffer, 0, read);
				partSize -= read;
				copied += read;
			}

			return copied;
		}

		private static int CopyPart(Stream sourceFile, string targetName, int partSize)
		{
			using var partFile = File.Create(targetName);
			return CopyPart(sourceFile, partFile, partSize);
		}
		
		private static void CopyPart(string sourceName, Stream targetFile)
		{
			using var sourceFile = File.OpenRead(sourceName);
			sourceFile.CopyTo(targetFile);
		}
	}
}
