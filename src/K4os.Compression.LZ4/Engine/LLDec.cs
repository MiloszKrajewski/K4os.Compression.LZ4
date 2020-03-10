#pragma warning disable 1591
// ReSharper disable IdentifierTypo
// ReSharper disable InconsistentNaming

using System.Runtime.CompilerServices;

namespace K4os.Compression.LZ4.Engine
{
	internal unsafe class LLDec
	{
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static int LZ4_decompress_safe(
			byte* source, byte* target, int sourceLength, int targetLength) =>
			LLTools.Algorithm32
				? LLDec32.LZ4_decompress_safe(source, target, sourceLength, targetLength)
				: LLDec64.LZ4_decompress_safe(source, target, sourceLength, targetLength);

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static int LZ4_decompress_safe_continue(
			LLTypes.LZ4_streamDecode_t* context,
			byte* source, byte* target, int sourceLength, int targetLength) =>
			LLTools.Algorithm32
				? LLDec32.LZ4_decompress_safe_continue(
					context, source, target, sourceLength, targetLength)
				: LLDec64.LZ4_decompress_safe_continue(
					context, source, target, sourceLength, targetLength);
	}
}
