using System.IO;
using System.Text;
using K4os.Compression.LZ4.Encoders;
using Xunit;

namespace K4os.Compression.LZ4.Test
{
	public class StreamingRoundtripTests
	{
		[Theory]
		[InlineData(".corpus/dickens", 4243, 0x10000, 8, 8)]
		[InlineData(".corpus/dickens", 0x10000 - 1, 0x10000, 8, 8)]
		[InlineData(".corpus/dickens", 0x10000 + 1, 0x10000, 8, 8)]
		public void TopupSize(
			string filename,
			int topupSize, int blockSize,
			int encoderExtraBlocks, int decoderExtraBlocks)
		{
			Roundtrip(filename, topupSize, blockSize, encoderExtraBlocks, decoderExtraBlocks);
		}

		[Theory]
		[InlineData(".corpus/dickens", 4096, 0x10000, 0, 0)]
		[InlineData(".corpus/dickens", 4096, 0x10000, 0, 3)]
		[InlineData(".corpus/dickens", 4096, 0x10000, 3, 0)]
		[InlineData(".corpus/dickens", 4096, 0x10000, 8, 8)]
		[InlineData(".corpus/samba", 4096, 0x10000, 0, 0)]
		[InlineData(".corpus/samba", 4096, 0x10000, 0, 3)]
		[InlineData(".corpus/samba", 4096, 0x10000, 3, 0)]
		[InlineData(".corpus/samba", 4096, 0x10000, 8, 8)]
		public void ExtraBlocks(
			string filename,
			int topupSize, int blockSize,
			int encoderExtraBlocks, int decoderExtraBlocks)
		{
			Roundtrip(filename, topupSize, blockSize, encoderExtraBlocks, decoderExtraBlocks);
		}

		[Theory]
		//[InlineData(".corpus/dickens", 4096, 4096, 0, 0)]
		//[InlineData(".corpus/dickens", 4096, 16384, 0, 3)]
		//[InlineData(".corpus/dickens", 4096, 0x10000, 3, 0)]
		//[InlineData(".corpus/dickens", 4096, 0x20000, 2, 3)]
		//[InlineData(".corpus/dickens", 4096, 0x100000, 0, 0)]
		//[InlineData(".corpus/samba", 4096, 4096, 0, 0)]
		//[InlineData(".corpus/samba", 4096, 16384, 0, 3)]
		//[InlineData(".corpus/samba", 4096, 0x10000, 3, 0)]
		//[InlineData(".corpus/samba", 4096, 0x20000, 2, 3)]
		//[InlineData(".corpus/samba", 4096, 0x100000, 0, 0)]
		[InlineData(".corpus/x-ray", 4096, 0x10000, 0, 0)]
		public void BlockSize(
			string filename,
			int topupSize, int blockSize,
			int encoderExtraBlocks, int decoderExtraBlocks)
		{
			Roundtrip(filename, topupSize, blockSize, encoderExtraBlocks, decoderExtraBlocks);
		}

		[Theory]
		[InlineData(".corpus/dickens")]
		[InlineData(".corpus/mozilla")]
		[InlineData(".corpus/mr")]
		[InlineData(".corpus/nci")]
		[InlineData(".corpus/ooffice")]
		[InlineData(".corpus/osdb")]
		[InlineData(".corpus/reymont")]
		[InlineData(".corpus/samba")]
		[InlineData(".corpus/sao")]
		[InlineData(".corpus/webster")]
		[InlineData(".corpus/xml")]
		[InlineData(".corpus/x-ray")]
		public void AllFiles(string filename)
		{
			Roundtrip(filename, 4096, 0x10000, 8, 8);
		}

		private static void Roundtrip(
			string filename, 
			int topupSize, int blockSize, 
			int encoderExtraBlocks, int decoderExtraBlocks)
		{
			var content = File.ReadAllBytes(Tools.FindFile(filename));
			var encoded = Encode(content, topupSize, blockSize, encoderExtraBlocks);
			var decoded = Decode(encoded, blockSize, decoderExtraBlocks);
			Tools.SameBytes(content, decoded);
		}

		private static byte[] Encode(byte[] input, int topupSize, int blockSize, int extraBlocks)
		{
			using (var outputStream = new MemoryStream())
			using (var inputStream = new MemoryStream(input))
			{
				using (var inputReader = new BinaryReader(inputStream, Encoding.UTF8, false))
				using (var outputWriter = new BinaryWriter(outputStream, Encoding.UTF8, true))
				using (var encoder = new LZ4FastStreamEncoder(blockSize, extraBlocks))
				{
					var inputBuffer = new byte[topupSize];
					var outputBuffer = new byte[LZ4Codec.MaximumOutputSize(encoder.BlockSize)];

					while (true)
					{
						var bytes = inputReader.Read(inputBuffer, 0, inputBuffer.Length);

						if (bytes == 0)
						{
							Flush(outputWriter, encoder, outputBuffer);
							outputWriter.Write(-1);
							break;
						}

						Write(outputWriter, encoder, inputBuffer, bytes, outputBuffer);
					}
				}

				return outputStream.ToArray();
			}
		}

		private static byte[] Decode(byte[] input, int blockSize, int extraBlocks)
		{
			using (var outputStream = new MemoryStream())
			using (var inputStream = new MemoryStream(input))
			{
				using (var inputReader = new BinaryReader(inputStream, Encoding.UTF8, false))
				using (var outputWriter = new BinaryWriter(outputStream, Encoding.UTF8, true))
				using (var decoder = new LZ4StreamDecoder(blockSize, extraBlocks))
				{
					var maximumInputBlock = LZ4Codec.MaximumOutputSize(blockSize);
					var inputBuffer = new byte[maximumInputBlock];
					var outputBuffer = new byte[blockSize];

					while (true)
					{
						var length = inputReader.ReadInt32();
						if (length < 0)
							break;

						Assert.True(length <= inputBuffer.Length);
						inputReader.Read(inputBuffer, 0, length);

						decoder.DecodeAndDrain(
							inputBuffer,
							0,
							length,
							outputBuffer,
							0,
							outputBuffer.Length,
							out var decoded);

						outputWriter.Write(outputBuffer, 0, decoded);
					}
				}

				return outputStream.ToArray();
			}
		}

		private static void Write(
			BinaryWriter outputWriter,
			ILZ4StreamEncoder encoder,
			byte[] inputBuffer, int bytes,
			byte[] outputBuffer)
		{
			var offset = 0;

			while (bytes > 0)
			{
				encoder.TopupAndEncode(
					inputBuffer,
					offset,
					bytes,
					outputBuffer,
					0,
					outputBuffer.Length,
					false,
					out var loaded,
					out var encoded);

				if (encoded > 0)
				{
					outputWriter.Write(encoded);
					outputWriter.Write(outputBuffer, 0, encoded);
				}

				bytes -= loaded;
				offset += loaded;
			}
		}

		private static void Flush(
			BinaryWriter outputWriter, ILZ4StreamEncoder encoder, byte[] outputBuffer)
		{
			if (encoder.BytesReady <= 0)
				return;

			var encoded = encoder.Encode(outputBuffer, 0, outputBuffer.Length);
			outputWriter.Write(encoded);
			outputWriter.Write(outputBuffer, 0, encoded);
		}
	}
}
