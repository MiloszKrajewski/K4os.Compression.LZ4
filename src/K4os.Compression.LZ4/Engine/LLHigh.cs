// ReSharper disable IdentifierTypo
// ReSharper disable InconsistentNaming

using System.Runtime.CompilerServices;
using K4os.Compression.LZ4.Internal;

namespace K4os.Compression.LZ4.Engine
{
	internal unsafe class LLHigh: LLTools
	{
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static uint HASH_FUNCTION(uint value) =>
			(value * 2654435761U) >> (MINMATCH * 8 - LZ4HC_HASH_LOG);

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static uint LZ4HC_hashPtr(void* ptr) => HASH_FUNCTION(Mem.Peek4(ptr));

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static ref ushort DELTANEXTU16(ushort* table, int pos) => ref table[(ushort) (pos)];
	}
}
