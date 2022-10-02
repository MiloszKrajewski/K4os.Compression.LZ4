using System;
using K4os.Compression.LZ4.Encoders;

namespace K4os.Compression.LZ4.Streams;

/// <summary>
/// Utility methods for LZ4 streams.
/// </summary>
public static class Extensions
{
	private static int ExtraBlocks(int blockSize, int extraMemory) =>
		Math.Max(extraMemory > 0 ? blockSize : 0, extraMemory) / blockSize;

	/// <summary>
	/// Creates <see cref="ILZ4Encoder"/> using <see cref="ILZ4Descriptor"/>.
	/// </summary>
	/// <param name="descriptor">LZ4 descriptor.</param>
	/// <param name="level">Compression level.</param>
	/// <param name="extraMemory">Additional memory for encoder.</param>
	/// <returns>Encoder.</returns>
	public static ILZ4Encoder CreateEncoder(
		this ILZ4Descriptor descriptor,
		LZ4Level level = LZ4Level.L00_FAST,
		int extraMemory = 0) =>
		LZ4Encoder.Create(
			descriptor.Chaining,
			level,
			descriptor.BlockSize,
			ExtraBlocks(descriptor.BlockSize, extraMemory));

	/// <summary>
	/// Creates <see cref="ILZ4Encoder"/> using <see cref="ILZ4Descriptor"/> and <see cref="LZ4EncoderSettings"/>.
	/// </summary>
	/// <param name="descriptor">LZ4 descriptor.</param>
	/// <param name="settings">Encoder settings.</param>
	/// <returns>Encoder.</returns>
	public static ILZ4Encoder CreateEncoder(
		this ILZ4Descriptor descriptor, LZ4EncoderSettings settings) =>
		LZ4Encoder.Create(
			descriptor.Chaining,
			settings.CompressionLevel,
			descriptor.BlockSize,
			ExtraBlocks(descriptor.BlockSize, settings.ExtraMemory));

//	/// <summary>
//	/// Creates <see cref="ILZ4Encoder"/> using <see cref="ILZ4Descriptor"/> and <see cref="LZ4EncoderSettings"/>.
//	/// </summary>
//	/// <param name="settings">Encoder settings.</param>
//	/// <returns>Encoder.</returns>
//	public static ILZ4Encoder CreateEncoder(
//		this LZ4EncoderSettings settings) =>
//		LZ4Encoder.Create(
//			settings.ChainBlocks,
//			settings.CompressionLevel,
//			settings.BlockSize,
//			ExtraBlocks(settings.BlockSize, settings.ExtraMemory));

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
