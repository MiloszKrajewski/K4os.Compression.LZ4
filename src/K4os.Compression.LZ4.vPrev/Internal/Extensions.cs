// ReSharper disable once CheckNamespace

namespace System
{
	internal static class Extensions
	{
		internal static void Validate<T>(this T[] buffer, int offset, int length)
		{
			if (buffer == null)
				throw new ArgumentNullException(
					nameof(buffer), "cannot be null");

			var valid = offset >= 0 && length >= 0 && offset + length <= buffer.Length;
			if (!valid)
				throw new ArgumentException(
					$"invalid offset/length combination: {offset}/{length}");
		}
	}
}
