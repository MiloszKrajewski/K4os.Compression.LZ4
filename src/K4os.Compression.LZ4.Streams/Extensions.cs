using System;
using K4os.Compression.LZ4.Encoders;

namespace K4os.Compression.LZ4.Streams;

public static class Extensions
{
	private static int ExtraBlocks(int blockSize, int extraMemory) =>
		Math.Max(extraMemory > 0 ? blockSize : 0, extraMemory) / blockSize;

	public static ILZ4Encoder CreateEncoder(
		this ILZ4Descriptor descriptor,
		LZ4Level level = LZ4Level.L00_FAST,
		int extraMemory = 0) =>
		LZ4Encoder.Create(
			descriptor.Chaining,
			level,
			descriptor.BlockSize,
			ExtraBlocks(descriptor.BlockSize, extraMemory));

	public static ILZ4Encoder CreateEncoder(
		this ILZ4Descriptor descriptor, LZ4EncoderSettings settings) =>
		LZ4Encoder.Create(
			descriptor.Chaining,
			settings.CompressionLevel,
			descriptor.BlockSize,
			ExtraBlocks(descriptor.BlockSize, settings.ExtraMemory));

	public static ILZ4Decoder CreateDecoder(
		this ILZ4Descriptor descriptor, int extraMemory = 0) =>
		LZ4Decoder.Create(
			descriptor.Chaining,
			descriptor.BlockSize,
			ExtraBlocks(descriptor.BlockSize, extraMemory));

	public static ILZ4Decoder CreateDecoder(
		this ILZ4Descriptor descriptor, LZ4DecoderSettings settings) =>
		LZ4Decoder.Create(
			descriptor.Chaining,
			descriptor.BlockSize,
			ExtraBlocks(descriptor.BlockSize, settings.ExtraMemory));

	public static ILZ4Descriptor CreateDescriptor(
		this LZ4EncoderSettings settings) =>
		new LZ4Descriptor(
			settings.ContentLength,
			settings.ContentChecksum,
			settings.ChainBlocks,
			settings.BlockChecksum,
			settings.Dictionary,
			settings.BlockSize);
}
