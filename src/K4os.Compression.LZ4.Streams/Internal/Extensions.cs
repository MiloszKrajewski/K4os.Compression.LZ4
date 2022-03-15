using System;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace K4os.Compression.LZ4.Streams.Internal
{
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

		private static EndOfStreamException EndOfStream() =>
			new("Unexpected end of stream. Data might be corrupted.");

		public static int TryReadBlock(
			this Stream stream,
			byte[] buffer, int offset, int count, bool optional)
		{
			var progress = 0;
			while (count > 0)
			{
				var read = stream.Read(buffer, offset + progress, count);

				if (read == 0)
					return progress == 0 && optional ? 0 : throw EndOfStream();

				progress += read;
				count -= read;
			}

			return progress;
		}

		public static async Task<int> TryReadBlockAsync(
			this Stream stream,
			byte[] buffer, int offset, int count, bool optional,
			CancellationToken token)
		{
			var progress = 0;
			while (count > 0)
			{
				var read = await stream
					.ReadAsync(buffer, offset + progress, count, token)
					.Weave();

				if (read == 0)
					return progress == 0 && optional ? 0 : throw EndOfStream();

				progress += read;
				count -= read;
			}

			return progress;
		}
	}
}
