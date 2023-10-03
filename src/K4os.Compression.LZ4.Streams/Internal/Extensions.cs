using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
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
	
	/// <summary>
	/// Asserts that given argument is not null. As it is designed to be used only in
	/// situations when we 100% ure that value is not null, it actually does anything
	/// only in DEBUG builds and has no effect in RELEASE. Mostly used to ensure
	/// static analysis tools that we know what we are doing.
	/// </summary>
	/// <param name="value">Argument value.</param>
	/// <param name="name">Name of argument.</param>
	/// <typeparam name="T">Type of argument.</typeparam>
	[Conditional("DEBUG")]
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static void AssertIsNotNull<T>(
		[NotNull] this T? value, [CallerArgumentExpression("value")] string? name = null) 
		where T: class
	{
		if (value is null) ThrowArgumentNullException(name);
	}

	[DoesNotReturn]
	[MethodImpl(MethodImplOptions.NoInlining)]
	private static void ThrowArgumentNullException(string? name) => 
		throw new ArgumentNullException(name);
}
