using System.Runtime.CompilerServices;

//------------------------------------------------------------------------------

// ReSharper disable IdentifierTypo
// ReSharper disable InconsistentNaming
// ReSharper disable AccessToStaticMemberViaDerivedType
// ReSharper disable ConditionIsAlwaysTrueOrFalse
// ReSharper disable BuiltInTypeReferenceStyle

//------------------------------------------------------------------------------

namespace K4os.Compression.LZ4.Engine
{
	internal static unsafe class LLxx
	{
		public static int LZ4_decompress_safe(
			byte* source, byte* target, int sourceLength, int targetLength) =>
			LL.Algorithm32
				? LL32.LZ4_decompress_safe(
					source, target, sourceLength, targetLength)
				: LL64.LZ4_decompress_safe(
					source, target, sourceLength, targetLength);

		public static int LZ4_decompress_safe_continue(
			LL.LZ4_streamDecode_t* context,
			byte* source, byte* target, int sourceLength, int targetLength) =>
			LL.Algorithm32
				? LL32.LZ4_decompress_safe_continue(
					context, source, target, sourceLength, targetLength)
				: LL64.LZ4_decompress_safe_continue(
					context, source, target, sourceLength, targetLength);
		
		public static int LZ4_compress_fast(
			byte* source, byte* target, int sourceLength, int targetLength,
			int acceleration) =>
			LL.Algorithm32
				? LL32.LZ4_compress_fast(
					source, target, sourceLength, targetLength, acceleration)
				: LL64.LZ4_compress_fast(
					source, target, sourceLength, targetLength, acceleration);

		public static int LZ4_compress_fast_continue(
			LL.LZ4_stream_t* context,
			byte* source, byte* target, int sourceLength, int targetLength,
			int acceleration) =>
			LL.Algorithm32
				? LL32.LZ4_compress_fast_continue(
					context,
					source, target, sourceLength, targetLength,
					acceleration)
				: LL64.LZ4_compress_fast_continue(
					context,
					source, target, sourceLength, targetLength,
					acceleration);

		public static int LZ4_compress_HC(
			byte* source, byte* target, int sourceLength, int targetLength, int level) =>
			LL.Algorithm32
				? LL32.LZ4_compress_HC(
					source, target, sourceLength, targetLength, level)
				: LL64.LZ4_compress_HC(
					source, target, sourceLength, targetLength, level);

		public static int LZ4_compress_HC_continue(
			LL.LZ4_streamHC_t* context, 
			byte* source, byte* target, int sourceLength, int targetLength) =>
			LL.Algorithm32
				? LL32.LZ4_compress_HC_continue(
					context, source, target, sourceLength, targetLength)
				: LL64.LZ4_compress_HC_continue(
					context, source, target, sourceLength, targetLength);
	}
}
