using System;
using System.Collections;
using System.IO;

namespace K4os.Compression.LZ4.Test
{
	public class Tools
	{
		public static uint Adler32(byte[] data)
		{
			const uint modAdler = 65521;

			uint a = 1, b = 0;
			var len = data.Length;

			for (var index = 0; index < len; ++index)
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

		public static void SameBytes(byte[] source, byte[] target)
		{
			if (source.Length != target.Length)
				throw new ArgumentException($"Arrays are not same length: {source.Length} vs {target.Length}");

			var length = source.Length;
			for (var i = 0; i < length; i++)
			{
				if (source[i] != target[i])
					throw new ArgumentException($"Arrays differ at index {i}: {source[i]} vs {target[i]}");
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
					throw new ArgumentException($"Arrays differ at index {i}: {source[i]} vs {target[i]}");
			}
		}
	}
}
