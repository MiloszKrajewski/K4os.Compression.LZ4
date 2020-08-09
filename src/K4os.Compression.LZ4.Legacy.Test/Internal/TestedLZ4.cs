using System.IO;
using K4os.Compression.LZ4.Internal;

namespace K4os.Compression.LZ4.Legacy.Test.Internal
{
	public class LZ4Settings
	{
		public LZ4Level Level { get; set; } = LZ4Level.L00_FAST;
		public int BlockSize { get; set; } = Mem.K64;
		public int ExtraBlocks { get; set; } = 0;
		public bool Chaining { get; set; } = true;
	}

	public class TestedLZ4
	{
		public static void Encode(
			string original, string encoded, bool high, int block, int chunk)
		{
			using (var input = File.OpenRead(original))
			using (var encode = LZ4Legacy.Encode(File.Create(encoded), high, block))
			{
				var buffer = new byte[chunk];
				while (true)
				{
					var read = input.Read(buffer, 0, buffer.Length);
					if (read == 0)
						break;

					encode.Write(buffer, 0, read);
				}
			}
		}
		
		public static void Decode(string encoded, string decoded, int chunkSize)
		{
			using (var source = LZ4Legacy.Decode(File.OpenRead(encoded)))
			using (var target = File.Create(decoded))
			{
				var buffer = new byte[chunkSize];
				while (true)
				{
					var read = source.Read(buffer, 0, buffer.Length);
					if (read == 0)
						break;

					target.Write(buffer, 0, read);
				}
			}
		}
	}
}
