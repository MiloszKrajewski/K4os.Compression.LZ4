using System;
using System.Buffers;
using System.IO;
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
	internal static ReadOnlySpan<T> AsReadOnly<T>(this Span<T> span) => span;
	internal static ReadOnlyMemory<T> AsReadOnly<T>(this Memory<T> memory) => memory;

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

	/// <summary>
	/// Create <see cref="ILZ4Decoder"/> using <see cref="ILZ4Descriptor"/>.
	/// </summary>
	/// <param name="descriptor">Descriptor.</param>
	/// <param name="extraMemory">Extra memory (may improves speed, but creates memory pressure).</param>
	/// <returns><see cref="ILZ4Decoder"/>.</returns>
	public static ILZ4Decoder CreateDecoder(
		this ILZ4Descriptor descriptor, int extraMemory = 0) =>
		LZ4Decoder.Create(
			descriptor.Chaining,
			descriptor.BlockSize,
			ExtraBlocks(descriptor.BlockSize, extraMemory));

	/// <summary>
	/// Create <see cref="ILZ4Decoder"/> using <see cref="ILZ4Descriptor"/> and <see cref="LZ4DecoderSettings"/>.
	/// </summary>
	/// <param name="descriptor">Descriptor.</param>
	/// <param name="settings">Settings.</param>
	/// <returns><see cref="ILZ4Decoder"/>.</returns>
	public static ILZ4Decoder CreateDecoder(
		this ILZ4Descriptor descriptor, LZ4DecoderSettings settings) =>
		LZ4Decoder.Create(
			descriptor.Chaining,
			descriptor.BlockSize,
			ExtraBlocks(descriptor.BlockSize, settings.ExtraMemory));

	/// <summary>
	/// Creates <see cref="ILZ4Descriptor"/> from <see cref="LZ4DecoderSettings"/>.
	/// </summary>
	/// <param name="settings">Settings.</param>
	/// <returns>LZ4 Descriptor.</returns>
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

	/// <summary>
	/// Copies all bytes from <see cref="ILZ4FrameReader"/> into <see cref="IBufferWriter{T}"/>.
	/// </summary>
	/// <param name="source">Frame reader.</param>
	/// <param name="target">Buffer writer.</param>
	/// <param name="blockSize">Chunk size.</param>
	/// <typeparam name="TBufferWriter">Type of buffer writer.</typeparam>
	public static void CopyTo<TBufferWriter>(
		this ILZ4FrameReader source, TBufferWriter target, int blockSize = 0)
		where TBufferWriter: IBufferWriter<byte>
	{
		blockSize = Math.Max(blockSize, 4096);
		while (true)
		{
			var span = target.GetSpan(blockSize);
			var bytes = source.ReadManyBytes(span, true);
			if (bytes == 0) return;

			target.Advance(bytes);
		}
	}

	/// <summary>
	/// Copies all bytes from <see cref="ILZ4FrameReader"/> into <see cref="IBufferWriter{T}"/>.
	/// </summary>
	/// <param name="source">LZ4 frame reader.</param>
	/// <param name="target">Buffer writer.</param>
	/// <param name="blockSize">Chunk size.</param>
	/// <typeparam name="TBufferWriter">Type of buffer writer.</typeparam>
	public static async Task CopyToAsync<TBufferWriter>(
		this ILZ4FrameReader source, TBufferWriter target, int blockSize = 0)
		where TBufferWriter: IBufferWriter<byte>
	{
		blockSize = Math.Max(blockSize, 4096);
		while (true)
		{
			var span = target.GetMemory(blockSize);
			var bytes = await source.ReadManyBytesAsync(span, true);
			if (bytes == 0) return;

			target.Advance(bytes);
		}
	}

	/// <summary>
	/// Copies all bytes from <see cref="ReadOnlySequence{T}"/> into <see cref="ILZ4FrameWriter"/>.
	/// </summary>
	/// <param name="target">Frame writer.</param>
	/// <param name="source">Sequence of bytes.</param>
	public static void CopyFrom(
		this ILZ4FrameWriter target, ReadOnlySequence<byte> source)
	{
		while (true)
		{
			if (source.IsEmpty) break;

			var bytes = source.First;
			source = source.Slice(bytes.Length);
			if (bytes.IsEmpty) continue;

			target.WriteManyBytes(bytes.Span);
		}
	}

	/// <summary>
	/// Copies all bytes from <see cref="ReadOnlySequence{T}"/> into <see cref="ILZ4FrameWriter"/>.
	/// </summary>
	/// <param name="target">Frame writer.</param>
	/// <param name="source">Sequence of bytes.</param>
	public static async Task CopyFromAsync(
		this ILZ4FrameWriter target, ReadOnlySequence<byte> source)
	{
		while (true)
		{
			if (source.IsEmpty) break;

			var bytes = source.First;
			source = source.Slice(bytes.Length);
			if (bytes.IsEmpty) continue;

			await target.WriteManyBytesAsync(bytes);
		}
	}

	/// <summary>
	/// Wraps <see cref="ILZ4FrameReader"/> as <see cref="Stream"/>.
	/// </summary>
	/// <param name="reader">LZ4 frame reader.</param>
	/// <param name="leaveOpen">Indicates that frame reader should be left open even if stream is
	/// disposed.</param>
	/// <param name="interactive">Indicates that data should be provided to reader as quick as
	/// possible, instead of waiting for whole block to be read.</param>
	/// <returns><see cref="LZ4FrameReaderAsStream"/> stream wrapper.</returns>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static LZ4FrameReaderAsStream AsStream(
		this ILZ4FrameReader reader, bool leaveOpen = false, bool interactive = false) =>
		new(reader, leaveOpen, interactive);

	/// <summary>
	/// Wraps <see cref="ILZ4FrameWriter"/> as <see cref="Stream"/>.
	/// </summary>
	/// <param name="writer">LZ4 frame writer.</param>
	/// <param name="leaveOpen">Indicates that frame writer should be left open even if stream is
	/// disposed.</param>
	/// <returns><see cref="LZ4FrameWriterAsStream"/> stream wrapper.</returns>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static LZ4FrameWriterAsStream AsStream(
		this ILZ4FrameWriter writer, bool leaveOpen = false) =>
		new(writer, leaveOpen);
}
