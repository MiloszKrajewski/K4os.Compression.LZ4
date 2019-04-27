using System;
using System.IO;
using RefStream = LZ4.LZ4Stream;
using RefStreamMode = LZ4.LZ4StreamMode;
using RefStreamFlags = LZ4.LZ4StreamFlags;

namespace K4os.Compression.LZ4.Legacy.Test.Internal
{
	public class ReferenceLZ4
	{
		public static void Encode(string original, string encoded, bool high, int block, int chunk)
		{
			var flags = high ? RefStreamFlags.HighCompression : RefStreamFlags.None;

			using (var input = File.OpenRead(original))
			using (var encode = new RefStream(File.Create(encoded), RefStreamMode.Compress, flags))
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

		public static void Decode(string encoded, string decoded, int chunk)
		{
			using (var source = new RefStream(File.OpenRead(encoded), RefStreamMode.Decompress))
			using (var target = File.Create(decoded))
			{
				var buffer = new byte[chunk];
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
