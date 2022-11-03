#nullable enable

using System;
using System.Buffers;
using System.IO;
using K4os.Compression.LZ4.Streams.Abstractions;
using K4os.Compression.LZ4.Streams.Adapters;
using K4os.Compression.LZ4.Streams.Frames;

#if NET5_0_OR_GREATER
using System.IO.Pipelines;
#endif

namespace K4os.Compression.LZ4.Streams;

/// <summary>
/// LZ4 factory methods to encode/decode anything which can be represented as a stream-like object.
/// Please note, to avoid all the complexity of dealing with streams, it uses
/// <see cref="ILZ4FrameReader"/> and <see cref="ILZ4FrameWriter"/> as stream abstractions.
/// </summary>
public static partial class LZ4Frame
{
	/// <summary>Creates decompression stream on top of inner stream.</summary>
	/// <param name="source">Span to read from.</param>
	/// <param name="target">Buffer to write to.</param>
	/// <param name="extraMemory">Extra memory used for decompression.</param>
	public static unsafe void Decode<TBufferWriter>(
		ReadOnlySpan<byte> source, TBufferWriter target, int extraMemory = 0)
		where TBufferWriter: IBufferWriter<byte>
	{
		fixed (byte* source0 = source)
		{
			using var decoder = new ByteSpanLZ4FrameReader(
				UnsafeByteSpan.Create(source0, source.Length),
				i => i.CreateDecoder(extraMemory));
			decoder.CopyTo(target);
		}
	}

	/// <summary>Creates decompression stream on top of inner stream.</summary>
	/// <param name="memory">Stream to be decoded.</param>
	/// <param name="extraMemory">Extra memory used for decompression.</param>
	/// <returns>Decompression stream.</returns>
	public static ByteMemoryLZ4FrameReader Decode(
		ReadOnlyMemory<byte> memory, int extraMemory = 0) =>
		new(memory, i => i.CreateDecoder(extraMemory));

	/// <summary>Creates decompression stream on top of inner stream.</summary>
	/// <param name="sequence">Stream to be decoded.</param>
	/// <param name="extraMemory">Extra memory used for decompression.</param>
	/// <returns>Decompression stream.</returns>
	public static ByteSequenceLZ4FrameReader Decode(
		ReadOnlySequence<byte> sequence, int extraMemory = 0) =>
		new(sequence, i => i.CreateDecoder(extraMemory));

	/// <summary>Creates decompression stream on top of inner stream.</summary>
	/// <param name="stream">Stream to be decoded.</param>
	/// <param name="extraMemory">Extra memory used for decompression.</param>
	/// <param name="leaveOpen">Indicates if stream should stay open after disposing decoder.</param>
	/// <returns>Decompression stream.</returns>
	public static StreamLZ4FrameReader Decode(
		Stream stream, int extraMemory = 0, bool leaveOpen = false) =>
		new(stream, leaveOpen, i => i.CreateDecoder(extraMemory));

	#if NET5_0_OR_GREATER

	/// <summary>Creates decompression stream on top of inner stream.</summary>
	/// <param name="reader">Stream to be decoded.</param>
	/// <param name="extraMemory">Extra memory used for decompression.</param>
	/// <param name="leaveOpen">Indicates if stream should stay open after disposing decoder.</param>
	/// <returns>Decompression stream.</returns>
	public static PipeLZ4FrameReader Decode(
		PipeReader reader, int extraMemory = 0, bool leaveOpen = false) =>
		new(reader, leaveOpen, i => i.CreateDecoder(extraMemory));

	#endif
}
