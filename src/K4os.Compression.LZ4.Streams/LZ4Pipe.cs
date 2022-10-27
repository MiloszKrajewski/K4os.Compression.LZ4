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

public static class LZ4Pipe
{
	/// <summary>Creates decompression stream on top of inner stream.</summary>
	/// <param name="stream">Inner stream.</param>
	/// <param name="buffer">Buffer writer to write to.</param>
	/// <param name="extraMemory">Extra memory used for decompression.</param>
	/// <returns>Decompression stream.</returns>
	public static unsafe void Decode<TBufferWriter>(
		Span<byte> stream, TBufferWriter buffer, int extraMemory = 0)
		where TBufferWriter: IBufferWriter<byte>
	{
		fixed (byte* stream0 = stream)
		{
			using var decoder = new ByteSpanLZ4FrameReader(
				new UnsafeByteSpan(new UIntPtr(stream0), stream.Length), 
				i => i.CreateDecoder(extraMemory));
			decoder.CopyTo(buffer);
		}
	}

	/// <summary>Creates decompression stream on top of inner stream.</summary>
	/// <param name="stream">Inner stream.</param>
	/// <param name="extraMemory">Extra memory used for decompression.</param>
	/// <returns>Decompression stream.</returns>
	public static ByteMemoryLZ4FrameReader Decode(
		ReadOnlyMemory<byte> stream, int extraMemory = 0) =>
		new(stream, i => i.CreateDecoder(extraMemory));

	/// <summary>Creates decompression stream on top of inner stream.</summary>
	/// <param name="stream">Inner stream.</param>
	/// <param name="extraMemory">Extra memory used for decompression.</param>
	/// <returns>Decompression stream.</returns>
	public static ByteSequenceLZ4FrameReader Decode(
		ReadOnlySequence<byte> stream, int extraMemory = 0) =>
		new(stream, i => i.CreateDecoder(extraMemory));

	/// <summary>Creates decompression stream on top of inner stream.</summary>
	/// <param name="stream">Inner stream.</param>
	/// <param name="leaveOpen">Indicates if stream should stay open after disposing decoder.</param>
	/// <param name="extraMemory">Extra memory used for decompression.</param>
	/// <returns>Decompression stream.</returns>
	public static StreamLZ4FrameReader Decode(
		Stream stream, bool leaveOpen = false, int extraMemory = 0) =>
		new(stream, leaveOpen, i => i.CreateDecoder(extraMemory));
	
	#if NET5_0_OR_GREATER
	
	/// <summary>Creates decompression stream on top of inner stream.</summary>
	/// <param name="reader">Inner stream.</param>
	/// <param name="leaveOpen">Indicates if stream should stay open after disposing decoder.</param>
	/// <param name="extraMemory">Extra memory used for decompression.</param>
	/// <returns>Decompression stream.</returns>
	public static PipeLZ4FrameReader Decode(
		PipeReader reader, bool leaveOpen = false, int extraMemory = 0) =>
		new(reader, leaveOpen, i => i.CreateDecoder(extraMemory));
	
	#endif
}