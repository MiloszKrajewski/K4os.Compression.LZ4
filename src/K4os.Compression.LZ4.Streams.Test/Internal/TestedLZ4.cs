using System.IO;
using K4os.Compression.LZ4.Encoders;

namespace K4os.Compression.LZ4.Streams.Test.Internal
{
	public class TestedLZ4
	{
		public static void Decode(string encoded, string decoded)
		{
			using (var input = File.OpenRead(encoded))
			using (var output = File.Create(decoded))
			using (var decode = new LZ4InputStream(
				input, i => new LZ4StreamDecoder(i.BlockSize, 0)))
			{
				var buffer = new byte[4096];
				while (true)
				{
					var read = decode.Read(buffer, 0, buffer.Length);
					if (read == 0)
						break;

					output.Write(buffer, 0, read);
				}
			}
		}

		public static void Encode(
			string original, string encoded,
			int chuckSize,
			bool chain, LZ4Level level, int blockSize, int extraBlocks)
		{
			var frameInfo = new LZ4FrameInfo(false, chain, false, null, blockSize);
			using (var input = File.OpenRead(original))
			using (var output = File.Create(encoded))
			using (var encode = new LZ4OutputStream(
				output, frameInfo, i => LZ4StreamEncoder.Create(level, i.BlockSize, extraBlocks)))
			{
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
}
