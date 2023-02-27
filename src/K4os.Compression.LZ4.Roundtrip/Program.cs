using System;
using System.Diagnostics;
using System.IO;
using System.IO.Hashing;
using K4os.Compression.LZ4.Internal;
using K4os.Compression.LZ4.Streams;

namespace K4os.Compression.LZ4.Roundtrip
{
	class Program
	{
		static void Main(string[] args)
		{
			foreach (var filename in args)
			foreach (var level in new[] {
				LZ4Level.L00_FAST,
				LZ4Level.L03_HC,
				LZ4Level.L09_HC,
				LZ4Level.L10_OPT,
				LZ4Level.L12_MAX,
			})
			{
				var temp = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
				Directory.CreateDirectory(temp);
				try
				{
					Roundtrip(temp, filename, level);
				}
				catch (Exception e)
				{
					Console.WriteLine($"{e.GetType().Name}: {e.Message}");
					Console.WriteLine(e.StackTrace);
				}
				finally
				{
					Directory.Delete(temp, true);
				}
			}
		}

		private static void Roundtrip(string temp, string filename, LZ4Level level)
		{
			Console.WriteLine($"Architecture: {IntPtr.Size * 8}bit");
			Console.WriteLine($"Roundtrip: {filename} @ {level}...");
			Console.WriteLine($"Storage: {temp}");

			var originalName = filename;
			var encodedName = Path.Combine(temp, "encoded.lz4");
			var decodedName = Path.Combine(temp, "decoded.lz4");

			using (var sourceFile = File.OpenRead(originalName))
			using (var targetFile = LZ4Stream.Encode(
				File.Create(encodedName), level, Mem.M1))
			{
				Console.WriteLine("Compression...");
				var stopwatch = Stopwatch.StartNew();
				sourceFile.CopyTo(targetFile);
				stopwatch.Stop();
				Console.WriteLine($"Time: {stopwatch.Elapsed.TotalMilliseconds:0.00}ms");
			}

			using (var sourceFile = LZ4Stream.Decode(
				File.OpenRead(encodedName), Mem.M1))
			using (var targetFile = File.Create(decodedName))
			{
				Console.WriteLine("Decompression...");
				var stopwatch = Stopwatch.StartNew();
				sourceFile.CopyTo(targetFile);
				stopwatch.Stop();
				Console.WriteLine($"Time: {stopwatch.Elapsed.TotalMilliseconds:0.00}ms");
			}

			using (var sourceFile = File.OpenRead(originalName))
			using (var targetFile = File.OpenRead(decodedName))
			{
				Console.WriteLine("Verification...");
				if (sourceFile.Length != targetFile.Length)
					throw new InvalidDataException("Files have different length");

				var sourceChecksum = Checksum(sourceFile);
				var targetChecksum = Checksum(targetFile);

				if (sourceChecksum != targetChecksum)
					throw new InvalidDataException("Files have different hash");
			}
		}

		private static uint Checksum(Stream file)
		{
			var hash = new XxHash32();
			var buffer = new byte[0x10000];
			while (true)
			{
				var read = file.Read(buffer, 0, buffer.Length);
				if (read == 0) break;

				hash.Append(buffer.AsSpan(0, read));
			}

			return hash.GetCurrentHashAsUInt32();
		}
	}
}
