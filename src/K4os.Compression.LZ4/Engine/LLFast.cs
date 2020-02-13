using System.Runtime.CompilerServices;
using K4os.Compression.LZ4.Engine_;
using K4os.Compression.LZ4.Internal;

namespace K4os.Compression.LZ4.Engine
{
	internal unsafe class LLFast
	{
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static int LZ4_compress_fast(
			byte* source, byte* target, int sourceLength, int targetLength,
			int acceleration) =>
			Mem.Is32Bit
				? LLFast32.LZ4_compress_fast(
					source, target, sourceLength, targetLength, acceleration)
				: LLFast64.LZ4_compress_fast(
					source, target, sourceLength, targetLength, acceleration);

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static int LZ4_compress_fast_continue(
			LLTypes.LZ4_stream_t* context,
			byte* source, byte* target, int sourceLength, int targetLength,
			int acceleration) =>
			Mem.Is32Bit
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
