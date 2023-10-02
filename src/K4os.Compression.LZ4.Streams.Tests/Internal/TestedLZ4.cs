using K4os.Compression.LZ4.Encoders;
using K4os.Compression.LZ4.Internal;

namespace K4os.Compression.LZ4.Streams.Tests.Internal
{
	public class LZ4Settings
	{
		public LZ4Level Level { get; set; } = LZ4Level.L00_FAST;
		public int BlockSize { get; set; } = Mem.K64;
		public int ExtraBlocks { get; set; } = 0;
		public bool Chaining { get; set; } = true;
		public bool BlockChecksum { get; set; } = false;
		public bool ContentChecksum { get; set; } = false;
	}

	public class TestedLZ4
	{
		public static void Decode(string encoded, string decoded, int chunkSize)
		{
			using var input = File.OpenRead(encoded);
			using var output = File.Create(decoded);
			using var decode = new LZ4DecoderStream(
				input, i => new LZ4ChainDecoder(i.BlockSize, 0));
			
			var buffer = new byte[chunkSize];
			while (true)
			{
				var read = decode.Read(buffer, 0, buffer.Length);
				if (read == 0)
					break;

				output.Write(buffer, 0, read);
			}
		}

		public static void Encode(
			string original, string encoded, int chuckSize, LZ4Settings settings)
		{
			var frameInfo = new LZ4Descriptor(
				null, settings.ContentChecksum, settings.Chaining, settings.BlockChecksum, 
				null, settings.BlockSize);
			using var input = File.OpenRead(original);
			using var output = File.Create(encoded);
			using var encode = new LZ4EncoderStream(
				output, frameInfo, i => i.CreateEncoder(settings.Level, settings.ExtraBlocks));
			var buffer = new byte[chuckSize];
			while (true)
			{
				var read = input.Read(buffer, 0, buffer.Length);
				if (read == 0)
					break;

				encode.Write(buffer, 0, read);
			}
		}
	}
}
