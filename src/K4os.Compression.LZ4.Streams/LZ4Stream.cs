using System;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using K4os.Compression.LZ4.Internal;

namespace K4os.Compression.LZ4.Streams;

/// <summary>
/// Utility class with factory methods to create LZ4 compression and decompression streams.
/// </summary>
public static class LZ4Stream
{
	#if NET45
	internal static readonly Task CompletedTask = Task.FromResult(0);
	#else
	internal static Task CompletedTask
	{
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => Task.CompletedTask;
	}
	#endif

	/// <summary>Created compression stream on top of inner stream.</summary>
	/// <param name="stream">Inner stream.</param>
	/// <param name="settings">Compression settings.</param>
	/// <param name="leaveOpen">Leave inner stream open after disposing.</param>
	/// <returns>Compression stream.</returns>
	public static LZ4EncoderStream Encode(
		Stream stream, LZ4EncoderSettings settings = null, bool leaveOpen = false)
	{
		settings ??= LZ4EncoderSettings.Default;
		var frameInfo = settings.CreateDescriptor();
		return new LZ4EncoderStream(stream, frameInfo, i => i.CreateEncoder(settings), leaveOpen);
	}

	/// <summary>Created compression stream on top of inner stream.</summary>
	/// <param name="stream">Inner stream.</param>
	/// <param name="level">Compression level.</param>
	/// <param name="extraMemory">Extra memory used for compression.</param>
	/// <param name="leaveOpen">Leave inner stream open after disposing.</param>
	/// <returns>Compression stream.</returns>
	public static LZ4EncoderStream Encode(
		Stream stream, LZ4Level level, int extraMemory = 0,
		bool leaveOpen = false)
	{
		var settings = new LZ4EncoderSettings {
			ChainBlocks = true,
			ExtraMemory = extraMemory,
			BlockSize = Mem.K64,
			CompressionLevel = level,
		};
		return Encode(stream, settings, leaveOpen);
	}

	/// <summary>Creates decompression stream on top of inner stream.</summary>
	/// <param name="stream">Inner stream.</param>
	/// <param name="settings">Decompression settings.</param>
	/// <param name="leaveOpen">Leave inner stream open after disposing.</param>
	/// <param name="interactive">If <c>true</c> reading from stream will be "interactive" allowing
	/// to read bytes as soon as possible, even if more data is expected.</param>
	/// <returns>Decompression stream.</returns>
	public static LZ4DecoderStream Decode(
		Stream stream,
		LZ4DecoderSettings settings = null,
		bool leaveOpen = false,
		bool interactive = false)
	{
		settings ??= LZ4DecoderSettings.Default;
		return new LZ4DecoderStream(
			stream, i => i.CreateDecoder(settings), leaveOpen, interactive);
	}

	/// <summary>Creates decompression stream on top of inner stream.</summary>
	/// <param name="stream">Inner stream.</param>
	/// <param name="extraMemory">Extra memory used for decompression.</param>
	/// <param name="leaveOpen">Leave inner stream open after disposing.</param>
	/// <param name="interactive">If <c>true</c> reading from stream will be "interactive" allowing
	/// to read bytes as soon as possible, even if more data is expected.</param>
	/// <returns>Decompression stream.</returns>
	public static LZ4DecoderStream Decode(
		Stream stream,
		int extraMemory,
		bool leaveOpen = false,
		bool interactive = false)
	{
		var settings = new LZ4DecoderSettings { ExtraMemory = extraMemory };
		return Decode(stream, settings, leaveOpen, interactive);
	}
}
