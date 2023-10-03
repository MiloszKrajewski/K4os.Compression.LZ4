using System.Collections.Generic;
using JetBrains.Annotations;

// ReSharper disable once CheckNamespace
namespace System;

public static class Extensions
{
	public static string Join(this IEnumerable<string> source, string separator) =>
		string.Join(separator, source);

	[CanBeNull]
	public static string NullIfEmpty([CanBeNull] this string value) =>
		string.IsNullOrWhiteSpace(value) ? null : value;
}
