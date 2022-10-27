using System;
using System.Buffers;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using K4os.Compression.LZ4.Encoders;
using K4os.Compression.LZ4.Streams.Abstractions;
using K4os.Compression.LZ4.Streams.Frames;

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
	
	/// <summary>Async version of <see cref="ILZ4FrameReader.OpenFrame"/>.</summary>
	/// <param name="reader">Decoder.</param>
	/// <returns><c>true</c> if frame was just opened,
	/// <c>false</c> if it was opened before.</returns>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Task<bool> OpenFrameAsync(this ILZ4FrameReader reader) =>
		reader.OpenFrameAsync(CancellationToken.None);

	/// <summary>Async version of <see cref="ILZ4FrameReader.GetFrameLength"/>.</summary>
	/// <param name="reader">Decoder.</param>
	/// <returns>Frame length, or <c>null</c></returns>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Task<long?> GetFrameLengthAsync(this ILZ4FrameReader reader) =>
		reader.GetFrameLengthAsync(CancellationToken.None);
	
	/// <summary>Reads one byte from LZ4 stream.</summary>
	/// <param name="reader">Decoder.</param>
	/// <returns>A byte, or -1 if end of stream.</returns>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Task<int> ReadOneByteAsync(this ILZ4FrameReader reader) =>
		reader.ReadOneByteAsync(CancellationToken.None);

	/// <summary>Reads many bytes from LZ4 stream. Return number of bytes actually read.</summary>
	/// <param name="reader">Decoder.</param>
	/// <param name="buffer">Byte buffer to read into.</param>
	/// <param name="interactive">if <c>true</c> then returns as soon as some bytes are read,
	/// if <c>false</c> then waits for all bytes being read or end of stream.</param>
	/// <returns>Number of bytes actually read.
	/// <c>0</c> means that end of stream has been reached.</returns>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Task<int> ReadManyBytesAsync(
		this ILZ4FrameReader reader, Memory<byte> buffer, bool interactive = false) =>
		reader.ReadManyBytesAsync(CancellationToken.None, buffer, interactive);
	
	/// <summary>
	/// Opens a stream by reading frame header. Please note, this methods can be called explicitly
	/// but does not need to be called, it will be called automatically if needed. 
	/// </summary>
	/// <param name="writer">Encoder.</param>
	/// <returns><c>true</c> if frame has been opened, or <c>false</c> if it was opened before.</returns>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Task<bool> OpenFrameAsync(this ILZ4FrameWriter writer) =>
		writer.OpenFrameAsync(CancellationToken.None);

	/// <summary>Writes one byte to stream.</summary>
	/// <param name="writer">Encoder.</param>
	/// <param name="value">Byte to be written.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Task WriteOneByteAsync(
		this ILZ4FrameWriter writer, byte value) =>
		writer.WriteOneByteAsync(CancellationToken.None, value);

	/// <summary>Writes multiple bytes to stream.</summary>
	/// <param name="writer">Encoder.</param>
	/// <param name="buffer">Byte buffer.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Task WriteManyBytesAsync(
		this ILZ4FrameWriter writer, ReadOnlyMemory<byte> buffer) =>
		writer.WriteManyBytesAsync(CancellationToken.None, buffer);

	/// <summary>
	/// Closes frame. Frame needs to be closed for stream to by valid, although
	/// this methods does not need to be called explicitly if stream is properly dispose.
	/// </summary>
	/// <param name="writer">Encoder.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Task CloseFrameAsync(this ILZ4FrameWriter writer) =>
		writer.CloseFrameAsync(CancellationToken.None);
	
	public static void CopyTo<TBufferWriter>(
		this ILZ4FrameReader reader, TBufferWriter buffer, int blockSize = 0)
		where TBufferWriter: IBufferWriter<byte>
	{
		blockSize = Math.Max(blockSize, 4096);
		while (true)
		{
			var span = buffer.GetSpan(blockSize);
			var bytes = reader.ReadManyBytes(span, true);
			if (bytes == 0) return;

			buffer.Advance(bytes);
		}
	}

	public static async Task CopyToAsync<TBufferWriter>(
		this ILZ4FrameReader reader, TBufferWriter buffer, int blockSize = 0)
		where TBufferWriter: IBufferWriter<byte>
	{
		blockSize = Math.Max(blockSize, 4096);
		while (true)
		{
			var span = buffer.GetMemory(blockSize);
			var bytes = await reader.ReadManyBytesAsync(span, true);
			if (bytes == 0) return;

			buffer.Advance(bytes);
		}
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static FrameDecoderAsStream AsStream(
		this ILZ4FrameReader reader, bool leaveOpen = false, bool interactive = false) =>
		new(reader, leaveOpen, interactive);

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static FrameEncoderAsStream AsStream(
		this ILZ4FrameWriter writer, bool leaveOpen = false) => 
		new(writer, leaveOpen);
}
