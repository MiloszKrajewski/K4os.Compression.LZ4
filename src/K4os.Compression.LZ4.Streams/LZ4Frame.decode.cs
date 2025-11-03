using System.Buffers;
using System.IO.Pipelines;
using K4os.Compression.LZ4.Streams.Abstractions;
using K4os.Compression.LZ4.Streams.Adapters;
using K4os.Compression.LZ4.Streams.Frames;

namespace K4os.Compression.LZ4.Streams;

/// <summary>
/// LZ4 factory methods to encode/decode anything which can be represented as a stream-like object.
/// Please note, to avoid all the complexity of dealing with streams, it uses
/// <see cref="ILZ4FrameReader"/> and <see cref="ILZ4FrameWriter"/> as stream abstractions.
/// </summary>
public static partial class LZ4Frame
{
#if NETSTANDARD2_0 || NET462
	// Simple buffer writer implementation for older frameworks
	private class SimpleBufferWriter: IBufferWriter<byte>
	{
		private byte[] _buffer;
		private int _position;

		public SimpleBufferWriter()
		{
			_buffer = new byte[4096];
			_position = 0;
		}

		public ReadOnlyMemory<byte> WrittenMemory => _buffer.AsMemory(0, _position);

		public void Advance(int count)
		{
			if (count < 0)
				throw new ArgumentOutOfRangeException(nameof(count));
			if (_position > _buffer.Length - count)
				throw new InvalidOperationException("Cannot advance past the end of the buffer.");
			_position += count;
		}

		public Memory<byte> GetMemory(int sizeHint = 0)
		{
			if (sizeHint < 0)
				throw new ArgumentOutOfRangeException(nameof(sizeHint));
			EnsureCapacity(sizeHint);
			return _buffer.AsMemory(_position);
		}

		public Span<byte> GetSpan(int sizeHint = 0) => GetMemory(sizeHint).Span;

		private void EnsureCapacity(int sizeHint)
		{
			var requiredSize = _position + sizeHint;
			if (_buffer.Length < requiredSize)
			{
				var newSize = Math.Max(_buffer.Length * 2, requiredSize);
				Array.Resize(ref _buffer, newSize);
			}
		}
	}
#endif

	/// <summary>Decompresses source bytes and returns the result as Memory.</summary>
	/// <param name="source">Compressed bytes to decode.</param>
	/// <param name="extraMemory">Extra memory used for decompression.</param>
	/// <returns>Decompressed data as Memory&lt;byte&gt;.</returns>
	public static Memory<byte> Decode(
		ReadOnlySpan<byte> source, int extraMemory = 0)
	{
#if NETSTANDARD2_0 || NET462
		var writer = new SimpleBufferWriter();
#else
		var writer = new ArrayBufferWriter<byte>();
#endif
		Decode(source, writer, extraMemory);
		return writer.WrittenMemory.ToArray();
	}

	/// <summary>Creates decompression stream on top of inner stream.</summary>
	/// <param name="source">Span to read from.</param>
	/// <param name="target">Buffer to write to.</param>
	/// <param name="extraMemory">Extra memory used for decompression.</param>
	public static unsafe TBufferWriter Decode<TBufferWriter>(
		ReadOnlySpan<byte> source, TBufferWriter target, int extraMemory = 0)
		where TBufferWriter: IBufferWriter<byte>
	{
		fixed (byte* source0 = source)
		{
			var decoder = new ByteSpanLZ4FrameReader(
				UnsafeByteSpan.Create(source0, source.Length),
				i => i.CreateDecoder(extraMemory));
			using (decoder) decoder.CopyTo(target);
			return target;
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

	/// <summary>Creates decompression stream on top of inner stream.</summary>
	/// <param name="reader">Stream to be decoded.</param>
	/// <param name="extraMemory">Extra memory used for decompression.</param>
	/// <param name="leaveOpen">Indicates if stream should stay open after disposing decoder.</param>
	/// <returns>Decompression stream.</returns>
	public static PipeLZ4FrameReader Decode(
		PipeReader reader, int extraMemory = 0, bool leaveOpen = false) =>
		new(reader, leaveOpen, i => i.CreateDecoder(extraMemory));
}
