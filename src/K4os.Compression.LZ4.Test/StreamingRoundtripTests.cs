using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using K4os.Compression.LZ4.Encoders;
using Xunit;

namespace K4os.Compression.LZ4.Test
{
	public class StreamingRoundtripTests
	{
		[Theory]
		[InlineData(".corpus/dickens")]
		public void Roundtrip(string filename)
		{
			using (var outputStream = new MemoryStream())
			{
				using (var outputWriter = new BinaryWriter(outputStream, Encoding.UTF8, true))
				using (var inputStream = new FileStream(Tools.FindFile(filename), FileMode.Open))
				using (var encoder = new LZ4FastStreamEncoder(0x10000, 8))
				{
					var inputBuffer = new byte[4096];
					var outputBuffer = new byte[LZ4Codec.MaximumOutputSize(encoder.BlockSize)];

					while (true)
					{
						int loaded;
						int encoded;

						var bytes = inputStream.Read(inputBuffer, 0, inputBuffer.Length);
						if (bytes == 0)
						{
							encoded = encoder.Encode(outputBuffer, 0, outputBuffer.Length);
						}
						else
						{
							encoder.TopupAndEncode(
								inputBuffer,
								0,
								bytes,
								outputBuffer,
								0,
								outputBuffer.Length,
								false,
								out loaded,
								out encoded);
						}

						if (encoded > 0)
						{
							outputWriter.Write(encoded);
							outputWriter.Write(outputBuffer, 0, encoded);
						}

						if (bytes == 0)
							break;
					}
				}
			}
		}
	}
}
