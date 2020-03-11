#pragma warning disable 1591
// ReSharper disable IdentifierTypo
// ReSharper disable InconsistentNaming

using System.Runtime.CompilerServices;

namespace K4os.Compression.LZ4.Engine
{
	internal unsafe class LLFast
	{
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static int LZ4_compress_fast(
			byte* source, byte* target, int sourceLength, int targetLength,
			int acceleration) =>
			LLTools.Algorithm32
				? LLFast32.LZ4_compress_fast(
					source, target, sourceLength, targetLength, acceleration)
				: LLFast64.LZ4_compress_fast(
					source, target, sourceLength, targetLength, acceleration);

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static int LZ4_compress_fast_continue(
			LLTypes.LZ4_stream_t* context,
			byte* source, byte* target, int sourceLength, int targetLength,
			int acceleration) =>
			LLTools.Algorithm32
				? LLFast32.LZ4_compress_fast_continue(
					context,
					source, target, sourceLength, targetLength,
					acceleration)
				: LLFast64.LZ4_compress_fast_continue(
					context,
					source, target, sourceLength, targetLength,
					acceleration);
	}
}
