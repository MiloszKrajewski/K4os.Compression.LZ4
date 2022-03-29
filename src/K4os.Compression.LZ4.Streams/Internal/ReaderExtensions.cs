using System.IO;
using System.Threading;
using System.Threading.Tasks;
using K4os.Compression.LZ4.Streams.Abstractions;

namespace K4os.Compression.LZ4.Streams.Internal;

internal static class ReaderExtensions
{
	private static EndOfStreamException EndOfStream() =>
		new("Unexpected end of stream. Data might be corrupted.");

	public static int TryReadBlock<TStream>(
		this TStream stream,
		byte[] buffer, int offset, int count, bool optional)
		where TStream: IStreamReader
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

	public static async Task<int> TryReadBlockAsync<TStream>(
		this TStream stream,
		byte[] buffer, int offset, int count, bool optional,
		CancellationToken token)
		where TStream: IStreamReader
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
