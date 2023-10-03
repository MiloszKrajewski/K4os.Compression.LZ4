#nullable enable

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

// ReSharper disable once CheckNamespace

namespace System;

internal static class Extensions
{
	internal static T Required<T>(
		[NotNull] this T? value,
		[CallerArgumentExpression("value")] string name = null!) =>
		value ?? ThrowArgumentNullException<T>(name);
	
	[Conditional("DEBUG")]
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static void AssertTrue(
		this bool value, 
		[CallerArgumentExpression("value")] string? name = null)
	{
		if (!value) ThrowAssertionFailed(name);
	}

	[DoesNotReturn]
	[MethodImpl(MethodImplOptions.NoInlining)]
	private static void ThrowAssertionFailed(string? name) =>
		throw new ArgumentException($"{name ?? "<unknown>"} assertion failed");
	
	[DoesNotReturn]
	[MethodImpl(MethodImplOptions.NoInlining)]
	private static T ThrowArgumentNullException<T>(string name) => 
		throw new ArgumentNullException(name);

	internal static void Validate<T>(
		this T[]? buffer, int offset, int length,
		bool allowNullIfEmpty = false)
	{
		if (allowNullIfEmpty && buffer is null && offset == 0 && length == 0)
			return;

		if (buffer is null)
			throw new ArgumentNullException(
				nameof(buffer), "cannot be null");

		var valid = offset >= 0 && length >= 0 && offset + length <= buffer.Length;
		if (!valid)
			throw new ArgumentException(
				$"invalid offset/length combination: {offset}/{length}");
	}
}
