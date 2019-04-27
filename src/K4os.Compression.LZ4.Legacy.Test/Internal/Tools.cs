using System;
using System.IO;
using Xunit;

namespace K4os.Compression.LZ4.Legacy.Test.Internal
{
	public class Tools
	{
		public static readonly string[] CorpusNames = {
			"dickens", "mozilla", "mr", "nci",
			"ooffice", "osdb", "reymont", "samba",
			"sao", "webster", "x-ray", "xml"
		};

		public static string FindFile(string filename) =>
			Path.Combine(FindRoot(), filename);

		private static string FindRoot(string path = ".")
		{
			bool IsRoot(string p) =>
				Path.GetFullPath(p) == Path.GetFullPath(Path.Combine(p, ".."));

			while (true)
			{
				if (Directory.Exists(Path.Combine(path, "./.git"))) return path;
				if (IsRoot(path)) return null;

				path = Path.Combine(path, "..");
			}
		}

		public static void SameBytes(byte[] source, byte[] target)
		{
			if (source.Length != target.Length)
				throw new ArgumentException(
					$"Arrays are not same length: {source.Length} vs {target.Length}");

			var length = source.Length;
			for (var i = 0; i < length; i++)
			{
				if (source[i] != target[i])
					throw new ArgumentException(
						$"Arrays differ at index {i}: {source[i]} vs {target[i]}");
			}
		}

		public static void SameBytes(byte[] source, byte[] target, int length)
		{
			if (source.Length < length)
				throw new ArgumentException($"Source array is too small: {source.Length}");
			if (target.Length < length)
				throw new ArgumentException($"Target array is too small: {target.Length}");

			for (var i = 0; i < length; i++)
			{
				if (source[i] != target[i])
					throw new ArgumentException(
						$"Arrays differ at index {i}: {source[i]} vs {target[i]}");
			}
		}

		public static void SameFiles(string original, string decoded)
		{
			using (var streamA = File.OpenRead(original))
			using (var streamB = File.OpenRead(decoded))
			{
				Assert.Equal(streamA.Length, streamB.Length);
				var bufferA = new byte[4096];
				var bufferB = new byte[4096];

				while (true)
				{
					var readA = streamA.Read(bufferA, 0, bufferA.Length);
					var readB = streamB.Read(bufferB, 0, bufferB.Length);
					Assert.Equal(readA, readB);
					if (readA == 0)
						break;

					SameBytes(bufferA, bufferB, readA);
				}
			}
		}

		public static void WriteRandom(string filename, int length, int seed = 0)
		{
			var random = new Random(seed);
			var buffer = new byte[0x10000];
			using (var file = File.Create(filename))
			{
				while (length > 0)
				{
					random.NextBytes(buffer);
					var chunkSize = Math.Min(length, buffer.Length);

					file.Write(buffer, 0, chunkSize);
					length -= chunkSize;
				}
			}
		}
	}
}
