using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace K4os.Compression.LZ4.Streams
{
	internal static class Extensions
	{
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static ConfiguredTaskAwaitable<T> Quick<T>(this Task<T> task) => 
			task.ConfigureAwait(false);
		
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static ConfiguredTaskAwaitable Quick(this Task task) => 
			task.ConfigureAwait(false);

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static T Quick<T>(this T value) => value;
		
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static ReadOnlySpan<byte> ToSpan(this ReadOnlySpan<byte> span) => span;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static ReadOnlySpan<byte> ToSpan(this ReadOnlyMemory<byte> span) => span.Span;
		
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static Span<byte> ToSpan(this Span<byte> span) => span;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static Span<byte> ToSpan(this Memory<byte> span) => span.Span;


	}
}
