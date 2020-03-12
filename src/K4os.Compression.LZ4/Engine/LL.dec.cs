// ReSharper disable IdentifierTypo
// ReSharper disable InconsistentNaming

using System.Runtime.CompilerServices;

namespace K4os.Compression.LZ4.Engine
{
	internal unsafe partial class LL
	{
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static int LZ4_decompress_safe(
			byte* source, byte* target, int sourceLength, int targetLength) =>
			Algorithm32
				? LL32.LZ4_decompress_safe(
					source, target, sourceLength, targetLength)
				: LL64.LZ4_decompress_safe(
					source, target, sourceLength, targetLength);

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static int LZ4_decompress_safe_continue(
			LZ4_streamDecode_t* context,
			byte* source, byte* target, int sourceLength, int targetLength) =>
			Algorithm32
				? LL32.LZ4_decompress_safe_continue(
					context, source, target, sourceLength, targetLength)
				: LL64.LZ4_decompress_safe_continue(
					context, source, target, sourceLength, targetLength);
	}
}
