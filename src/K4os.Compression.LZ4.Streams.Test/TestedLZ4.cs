using System.IO;
using K4os.Compression.LZ4.Encoders;

namespace K4os.Compression.LZ4.Streams.Test
{
	public class TestedLZ4
	{
		public static void Decode(string encoded, string decoded)
		{
			using (var input = File.OpenRead(encoded))
			using (var output = File.Create(decoded))
			using (var decode = new LZ4InputStream(input, i => new LZ4StreamDecoder(i.BlockSize, 0)))
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

		public static void Encode(string original, string encoded, ILZ4FrameInfo frameInfo)
		{
			using (var input = File.OpenRead(original))
			using (var output = File.Create(encoded))
			using (var encode = new LZ4OutputStream(output, frameInfo, i => new LZ4FastStreamEncoder(i.BlockSize)))
			{
				var buffer = new byte[4096];
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
