using System;
using System.IO;
using K4os.Compression.LZ4.Encoders;
using K4os.Compression.LZ4.Internal;

namespace K4os.Compression.LZ4.Streams
{
	public static class LZ4Stream
	{
		private static int ExtraBlocks(int blockSize, int extraMemory) =>
			Math.Max(0, extraMemory) / blockSize;

		public static LZ4DecoderStream Decode(Stream stream, int extraMemory = 0)
		{
			var settings = new LZ4DecoderSettings { ExtraMemory = extraMemory };
			return Decode(stream, settings);
		}

		public static LZ4DecoderStream Decode(Stream stream, LZ4DecoderSettings settings)
		{
			var extraMemory = settings.ExtraMemory;
			return new LZ4DecoderStream(
				stream,
				i => new LZ4Decoder(
					i.BlockSize, ExtraBlocks(i.BlockSize, extraMemory)));
		}

		public static LZ4EncoderStream Encode(Stream stream, LZ4EncoderSettings settings)
		{
			var frameInfo = new LZ4FrameInfo(
				settings.ContentLength,
				settings.ContentChecksum,
				settings.ChainBlocks,
				settings.BlockChecksum,
				settings.Dictionary,
				settings.BlockSize);
			var level = settings.CompressionLevel;
			var extraMemory = settings.ExtraMemory;

			return new LZ4EncoderStream(
				stream,
				frameInfo,
				i => LZ4Encoder.Create(
					level, i.BlockSize, ExtraBlocks(i.BlockSize, extraMemory)));
		}

		public static LZ4EncoderStream Encode(
			Stream stream, LZ4Level level = LZ4Level.L00_FAST, int extraMemory = 0)
		{
			var settings = new LZ4EncoderSettings {
				ChainBlocks = true,
				ExtraMemory = extraMemory,
				BlockSize = Mem.K64,
				CompressionLevel = level
			};
			return Encode(stream, settings);
		}
	}
}
