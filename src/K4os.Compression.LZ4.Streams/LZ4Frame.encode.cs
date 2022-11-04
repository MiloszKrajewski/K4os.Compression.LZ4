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

public static partial class LZ4Frame
{
	private static LZ4EncoderSettings ToEncoderSettings(LZ4Level level, int extraMemory) =>
		new() { CompressionLevel = level, ExtraMemory = extraMemory };

	/// <summary>
	/// Compresses source bytes into target buffer. Returns number of bytes actually written. 
	/// </summary>
	/// <param name="source">Source bytes.</param>
	/// <param name="target">Target buffer.</param>
	/// <param name="settings">Compression settings.</param>
	/// <returns>Number of bytes actually written.</returns>
	public static TBufferWriter Encode<TBufferWriter>(
		ReadOnlySequence<byte> source, TBufferWriter target,
		LZ4EncoderSettings? settings = default)
		where TBufferWriter: IBufferWriter<byte>
	{
		settings ??= LZ4EncoderSettings.Default;
		var encoder = new ByteBufferLZ4FrameWriter<TBufferWriter>(
			target,
			i => i.CreateEncoder(settings.CompressionLevel, settings.ExtraMemory),
			settings.CreateDescriptor());
		using (encoder) encoder.CopyFrom(source);
		return encoder.BufferWriter;
	}
	
	/// <summary>
	/// Compresses source bytes into target buffer. Returns number of bytes actually written. 
	/// </summary>
	/// <param name="source">Source bytes.</param>
	/// <param name="target">Target buffer.</param>
	/// <param name="settings">Compression settings.</param>
	/// <returns>Number of bytes actually written.</returns>
	public static TBufferWriter Encode<TBufferWriter>(
		ReadOnlySpan<byte> source, TBufferWriter target,
		LZ4EncoderSettings? settings = default)
		where TBufferWriter: IBufferWriter<byte>
	{
		settings ??= LZ4EncoderSettings.Default;
		var encoder = new ByteBufferLZ4FrameWriter<TBufferWriter>(
			target,
			i => i.CreateEncoder(settings.CompressionLevel, settings.ExtraMemory),
			settings.CreateDescriptor());
		using (encoder) encoder.WriteManyBytes(source);
		return encoder.BufferWriter;
	}

	/// <summary>
	/// Compresses source bytes into target buffer. Returns number of bytes actually written. 
	/// </summary>
	/// <param name="source">Source bytes.</param>
	/// <param name="target">Target buffer.</param>
	/// <param name="level">Compression level.</param>
	/// <param name="extraMemory">Extra memory.</param>
	/// <returns>Number of bytes actually written.</returns>
	public static TBufferWriter Encode<TBufferWriter>(
		ReadOnlySequence<byte> source, TBufferWriter target,
		LZ4Level level, int extraMemory = 0)
		where TBufferWriter: IBufferWriter<byte> =>
		Encode(source, target, ToEncoderSettings(level, extraMemory));
	
	/// <summary>
	/// Compresses source bytes into target buffer. Returns number of bytes actually written. 
	/// </summary>
	/// <param name="source">Source bytes.</param>
	/// <param name="target">Target buffer.</param>
	/// <param name="level">Compression level.</param>
	/// <param name="extraMemory">Extra memory.</param>
	/// <returns>Number of bytes actually written.</returns>
	public static TBufferWriter Encode<TBufferWriter>(
		ReadOnlySpan<byte> source, TBufferWriter target,
		LZ4Level level, int extraMemory = 0)
		where TBufferWriter: IBufferWriter<byte> =>
		Encode(source, target, ToEncoderSettings(level, extraMemory));

	/// <summary>
	/// Compresses source bytes into target buffer. Returns number of bytes actually written. 
	/// </summary>
	/// <param name="source">Source bytes.</param>
	/// <param name="target">Target buffer.</param>
	/// <param name="settings">Compression settings.</param>
	/// <returns>Number of bytes actually written.</returns>
	public static unsafe int Encode(
		ReadOnlySequence<byte> source, Span<byte> target,
		LZ4EncoderSettings? settings = default)
	{
		settings ??= LZ4EncoderSettings.Default;
		fixed (byte* stream0 = target)
		{
			using var encoder = new ByteSpanLZ4FrameWriter(
				UnsafeByteSpan.Create(stream0, target.Length),
				i => i.CreateEncoder(settings.CompressionLevel, settings.ExtraMemory),
				settings.CreateDescriptor());
			encoder.CopyFrom(source);
			return encoder.CompressedLength;
		}
	}

	/// <summary>
	/// Compresses source bytes into target buffer. Returns number of bytes actually written. 
	/// </summary>
	/// <param name="source">Source bytes.</param>
	/// <param name="target">Target buffer.</param>
	/// <param name="settings">Compression settings.</param>
	/// <returns>Number of bytes actually written.</returns>
	public static unsafe int Encode(
		Span<byte> source, Span<byte> target,
		LZ4EncoderSettings? settings = default)
	{
		settings ??= LZ4EncoderSettings.Default;
		fixed (byte* stream0 = target)
		{
			using var encoder = new ByteSpanLZ4FrameWriter(
				UnsafeByteSpan.Create(stream0, target.Length),
				i => i.CreateEncoder(settings.CompressionLevel, settings.ExtraMemory),
				settings.CreateDescriptor());
			encoder.WriteManyBytes(source);
			return encoder.CompressedLength;
		}
	}

	/// <summary>
	/// Writes bytes into target buffer. Returns number of bytes actually written. 
	/// </summary>
	/// <param name="source">Source of bytes, a function which write to LZ4 encoder.</param>
	/// <param name="target">Target buffer.</param>
	/// <param name="settings">Compression settings.</param>
	/// <returns>Number of bytes actually written.</returns>
	public static unsafe int Encode(
		Action<ILZ4FrameWriter> source, Span<byte> target,
		LZ4EncoderSettings? settings = default)
	{
		settings ??= LZ4EncoderSettings.Default;
		fixed (byte* stream0 = target)
		{
			using var encoder = new ByteSpanLZ4FrameWriter(
				UnsafeByteSpan.Create(stream0, target.Length),
				i => i.CreateEncoder(settings.CompressionLevel, settings.ExtraMemory),
				settings.CreateDescriptor());
			source(encoder);
			return encoder.CompressedLength;
		}
	}

	/// <summary>
	/// Compresses source bytes into target buffer. Returns number of bytes actually written. 
	/// </summary>
	/// <param name="source">Source bytes.</param>
	/// <param name="target">Target buffer.</param>
	/// <param name="level">Compression level.</param>
	/// <param name="extraMemory">Extra memory for encoder.</param>
	/// <returns>Number of bytes actually written.</returns>
	public static int Encode(
		ReadOnlySequence<byte> source, Span<byte> target,
		LZ4Level level, int extraMemory = 0) =>
		Encode(source, target, ToEncoderSettings(level, extraMemory));

	/// <summary>
	/// Compresses source bytes into target buffer. Returns number of bytes actually written. 
	/// </summary>
	/// <param name="source">Source bytes.</param>
	/// <param name="target">Target buffer.</param>
	/// <param name="level">Compression level.</param>
	/// <param name="extraMemory">Extra memory for encoder.</param>
	/// <returns>Number of bytes actually written.</returns>
	public static int Encode(
		Span<byte> source, Span<byte> target,
		LZ4Level level, int extraMemory = 0) =>
		Encode(source, target, ToEncoderSettings(level, extraMemory));

	/// <summary>
	/// Compresses source bytes into target buffer. Returns number of bytes actually written. 
	/// </summary>
	/// <param name="source">Source of bytes, a function which write to LZ4 encoder.</param>
	/// <param name="target">Target buffer.</param>
	/// <param name="level">Compression level.</param>
	/// <param name="extraMemory">Extra memory for encoder.</param>
	/// <returns>Number of bytes actually written.</returns>
	public static int Encode(
		Action<ILZ4FrameWriter> source, Span<byte> target,
		LZ4Level level, int extraMemory = 0) =>
		Encode(source, target, ToEncoderSettings(level, extraMemory));

	/// <summary>
	/// Create LZ4 encoder that writes compressed data into target buffer.
	/// Please note, target buffer needs to be pinned for the whole time encoder is used.
	/// This is definitely very unsafe method, and if you don't understand what it does,
	/// don't use it.
	/// </summary>
	/// <param name="target">Pointer to target buffer.</param>
	/// <param name="length">Length of target buffer.</param>
	/// <param name="settings">Encoder settings.</param>
	/// <returns>LZ4 frame writer.</returns>
	public static unsafe ByteSpanLZ4FrameWriter Encode(
		byte* target, int length,
		LZ4EncoderSettings? settings = default)
	{
		settings ??= LZ4EncoderSettings.Default;
		return new ByteSpanLZ4FrameWriter(
			UnsafeByteSpan.Create(target, length),
			i => i.CreateEncoder(settings.CompressionLevel, settings.ExtraMemory),
			settings.CreateDescriptor());
	}

	/// <summary>
	/// Create LZ4 encoder that writes compressed data into target buffer.
	/// Please note, target buffer needs to be pinned for the whole time encoder is used.
	/// This is definitely very unsafe method, and if you don't understand what it does,
	/// don't use it.
	/// </summary>
	/// <param name="target">Pointer to target buffer.</param>
	/// <param name="length">Length of target buffer.</param>
	/// <param name="level">Compression level.</param>
	/// <param name="extraMemory">Extra memory for encoder.</param>
	/// <returns>LZ4 frame writer.</returns>
	public static unsafe ByteSpanLZ4FrameWriter Encode(
		byte* target, int length,
		LZ4Level level, int extraMemory = 0) =>
		Encode(target, length, ToEncoderSettings(level, extraMemory));

	/// <summary>
	/// Create LZ4 encoder that writes compressed data into target buffer.
	/// </summary>
	/// <param name="target">Target buffer.</param>
	/// <param name="settings">Encoder settings.</param>
	/// <returns>LZ4 frame writer.</returns>
	public static ByteMemoryLZ4FrameWriter Encode(
		Memory<byte> target,
		LZ4EncoderSettings? settings = default)
	{
		settings ??= LZ4EncoderSettings.Default;
		return new ByteMemoryLZ4FrameWriter(
			target,
			i => i.CreateEncoder(settings.CompressionLevel, settings.ExtraMemory),
			settings.CreateDescriptor());
	}

	/// <summary>
	/// Create LZ4 encoder that writes compressed data into target buffer.
	/// </summary>
	/// <param name="target">Target buffer.</param>
	/// <param name="level">Compression level.</param>
	/// <param name="extraMemory">Extra memory for encoder.</param>
	/// <returns>LZ4 frame writer.</returns>
	public static ByteMemoryLZ4FrameWriter Encode(
		Memory<byte> target,
		LZ4Level level, int extraMemory = 0) =>
		Encode(target, ToEncoderSettings(level, extraMemory));

	/// <summary>
	/// Create LZ4 encoder that writes compressed data into target buffer.
	/// </summary>
	/// <param name="target">Target buffer.</param>
	/// <param name="settings">Encoder settings.</param>
	/// <typeparam name="TBufferWriter">Byte of buffer writer implementing <see cref="IBufferWriter{T}"/>.</typeparam>
	/// <returns>LZ4 frame writer.</returns>
	public static ByteBufferLZ4FrameWriter<TBufferWriter> Encode<TBufferWriter>(
		TBufferWriter target,
		LZ4EncoderSettings? settings = default)
		where TBufferWriter: IBufferWriter<byte>
	{
		settings ??= LZ4EncoderSettings.Default;
		return new ByteBufferLZ4FrameWriter<TBufferWriter>(
			target,
			i => i.CreateEncoder(settings.CompressionLevel, settings.ExtraMemory),
			settings.CreateDescriptor());
	}

	/// <summary>
	/// Create LZ4 encoder that writes compressed data into target buffer.
	/// </summary>
	/// <param name="target">Target buffer.</param>
	/// <param name="level">Compression level.</param>
	/// <param name="extraMemory">Extra memory for encoder.</param>
	/// <typeparam name="TBufferWriter">Byte of buffer writer implementing <see cref="IBufferWriter{T}"/>.</typeparam>
	/// <returns>LZ4 frame writer.</returns>
	public static ByteBufferLZ4FrameWriter<TBufferWriter> Encode<TBufferWriter>(
		TBufferWriter target,
		LZ4Level level, int extraMemory = 0)
		where TBufferWriter: IBufferWriter<byte> =>
		Encode(target, ToEncoderSettings(level, extraMemory));

	/// <summary>
	/// Create LZ4 encoder that writes compressed data into target buffer.
	/// </summary>
	/// <param name="target">Target buffer.</param>
	/// <param name="settings">Encoder settings.</param>
	/// <returns>LZ4 frame writer.</returns>
	public static ByteBufferLZ4FrameWriter Encode(
		IBufferWriter<byte> target,
		LZ4EncoderSettings? settings = default)
	{
		settings ??= LZ4EncoderSettings.Default;
		return new ByteBufferLZ4FrameWriter(
			target,
			i => i.CreateEncoder(settings.CompressionLevel, settings.ExtraMemory),
			settings.CreateDescriptor());
	}

	/// <summary>
	/// Create LZ4 encoder that writes compressed data into target buffer.
	/// </summary>
	/// <param name="target">Target buffer.</param>
	/// <param name="level">Compression level.</param>
	/// <param name="extraMemory">Extra memory for encoder.</param>
	/// <returns>LZ4 frame writer.</returns>
	public static ByteBufferLZ4FrameWriter Encode(
		IBufferWriter<byte> target,
		LZ4Level level, int extraMemory = 0) =>
		Encode(target, ToEncoderSettings(level, extraMemory));

	/// <summary>
	/// Create LZ4 encoder that writes compressed data into target stream.
	/// </summary>
	/// <param name="target">Target stream.</param>
	/// <param name="settings">Encoder settings.</param>
	/// <param name="leaveOpen">Leave target stream open after encoder is disposed.</param>
	/// <returns>LZ4 frame writer.</returns>
	public static StreamLZ4FrameWriter Encode(
		Stream target,
		LZ4EncoderSettings? settings = default,
		bool leaveOpen = false)
	{
		settings ??= LZ4EncoderSettings.Default;
		return new StreamLZ4FrameWriter(
			target,
			leaveOpen,
			i => i.CreateEncoder(settings.CompressionLevel, settings.ExtraMemory),
			settings.CreateDescriptor());
	}

	/// <summary>
	/// Create LZ4 encoder that writes compressed data into target stream.
	/// </summary>
	/// <param name="target">Target stream.</param>
	/// <param name="level">Compression level.</param>
	/// <param name="extraMemory">Extra memory for encoder.</param>
	/// <param name="leaveOpen">Leave target stream open after encoder is disposed.</param>
	/// <returns></returns>
	public static StreamLZ4FrameWriter Encode(
		Stream target,
		LZ4Level level, int extraMemory = 0,
		bool leaveOpen = false) =>
		Encode(target, ToEncoderSettings(level, extraMemory), leaveOpen);

	#if NET5_0_OR_GREATER
	/// <summary>
	/// Create LZ4 encoder that writes compressed data into target pipe.
	/// </summary>
	/// <param name="target">Target pipe.</param>
	/// <param name="settings">Encoder settings.</param>
	/// <param name="leaveOpen">Leave target pipe open after encoder is disposed.</param>
	/// <returns>LZ4 frame writer.</returns>
	public static PipeLZ4FrameWriter Encode(
		PipeWriter target,
		LZ4EncoderSettings? settings = default,
		bool leaveOpen = false)
	{
		settings ??= LZ4EncoderSettings.Default;
		return new PipeLZ4FrameWriter(
			target,
			leaveOpen,
			i => i.CreateEncoder(settings.CompressionLevel, settings.ExtraMemory),
			settings.CreateDescriptor());
	}
	
	/// <summary>
	/// Create LZ4 encoder that writes compressed data into target pipe.
	/// </summary>
	/// <param name="target">Target pipe.</param>
	/// <param name="level">Compression level.</param>
	/// <param name="extraMemory">Extra memory for encoder.</param>
	/// <param name="leaveOpen">Leave target pipe open after encoder is disposed.</param>
	/// <returns>LZ4 frame writer.</returns>
	public static PipeLZ4FrameWriter Encode(
		PipeWriter target,
		LZ4Level level, int extraMemory = 0,
		bool leaveOpen = false) =>
		Encode(target, ToEncoderSettings(level, extraMemory), leaveOpen);

	#endif
}
