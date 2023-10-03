using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace K4os.Compression.LZ4.Legacy;

internal static class Extensions
{
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
