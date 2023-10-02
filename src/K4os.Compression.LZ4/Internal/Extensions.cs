#nullable enable

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

	[DoesNotReturn]
	[MethodImpl(MethodImplOptions.NoInlining)]
	private static T ThrowArgumentNullException<T>(string argumentName) => 
		throw new ArgumentNullException(argumentName);

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
