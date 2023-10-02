#nullable enable

using System.Runtime.CompilerServices;

namespace K4os.Compression.LZ4.Streams.Internal;

internal static class Extensions
{
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static ConfiguredTaskAwaitable<T> Weave<T>(this Task<T> task) =>
		task.ConfigureAwait(false);

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static ConfiguredTaskAwaitable Weave(this Task task) =>
		task.ConfigureAwait(false);

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static ConfiguredValueTaskAwaitable<T> Weave<T>(this ValueTask<T> task) =>
		task.ConfigureAwait(false);

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static ConfiguredValueTaskAwaitable Weave(this ValueTask task) =>
		task.ConfigureAwait(false);

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static ReadOnlySpan<byte> ToSpan(this ReadOnlySpan<byte> span) => span;

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static ReadOnlySpan<byte> ToSpan(this ReadOnlyMemory<byte> span) => span.Span;

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Span<byte> ToSpan(this Span<byte> span) => span;

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Span<byte> ToSpan(this Memory<byte> span) => span.Span;
}
