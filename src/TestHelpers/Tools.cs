using System;
using System.IO;
using Xunit;

namespace TestHelpers
{
	public static class Tools
	{
		public static void Fill(this Random random, Span<byte> span)
		{
			for (var i = 0; i < span.Length; i++)
				span[i] = (byte) random.Next();
		}
		
		public static unsafe uint Adler32(byte* data, int length)
		{
			const uint modAdler = 65521;

			uint a = 1, b = 0;

			for (var index = 0; index < length; ++index)
			{
				a = (a + data[index]) % modAdler;
				b = (b + a) % modAdler;
			}

			return (b << 16) | a;
		}

		public static uint Adler32(byte[] data, int index = 0, int length = -1)
		{
			const uint modAdler = 65521;
			if (length < 0)
				length = data.Length - index;

			uint a = 1, b = 0;

			for (; index < length; ++index)
			{
				a = (a + data[index]) % modAdler;
				b = (b + a) % modAdler;
			}

			return (b << 16) | a;
		}

		public static byte[] LoadChunk(string filename, int index, int length)
		{
			using (var file = File.OpenRead(filename))
			{
				length = length < 0 ? (int) (file.Length - index) : length;
				var src = new byte[length];
				file.Seek(index, SeekOrigin.Begin);
				file.Read(src, 0, length);
				return src;
			}
		}

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

		public static Stream Slow(Stream stream, int threshold = 1) =>
			new FakeNetworkStream(stream, threshold);

		public static void SameBytes(ReadOnlySpan<byte> source, ReadOnlySpan<byte> target)
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
			SameBytes(source.AsSpan(0, length), target.AsSpan(0, length));
		}

		public static readonly string[] CorpusNames = {
			"dickens", "mozilla", "mr", "nci",
			"ooffice", "osdb", "reymont", "samba",
			"sao", "webster", "x-ray", "xml"
		};

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
		
		public static double NormExp(double value, double scale = 1.0) =>
				((Math.Exp(value * scale) - 1) / (Math.Exp(scale) - 1));

		public static double NextExp(this Random random, double scale = 1.0) =>
			NormExp(random.NextDouble(), scale);
	}
}
