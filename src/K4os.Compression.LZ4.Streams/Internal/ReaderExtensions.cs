using System.IO;
using System.Threading;
using System.Threading.Tasks;
using K4os.Compression.LZ4.Streams.Abstractions;

namespace K4os.Compression.LZ4.Streams.Internal;

internal static class ReaderExtensions
{
	private static EndOfStreamException EndOfStream() =>
		new("Unexpected end of stream. Data might be corrupted.");

	public static int TryReadBlock<TStreamReader, TStreamState>(
		this TStreamReader stream, ref TStreamState state,
		byte[] buffer, int offset, int count, bool optional)
		where TStreamReader: IStreamReader<TStreamState>
	{
		var progress = 0;
		while (count > 0)
		{
			var read = stream.Read(ref state, buffer, offset + progress, count);

			if (read == 0)
				return progress == 0 && optional ? 0 : throw EndOfStream();

			progress += read;
			count -= read;
		}

		return progress;
	}

	public static async Task<ReadResult<TStreamState>>
		TryReadBlockAsync<TStreamReader, TStreamState>(
			this TStreamReader stream, TStreamState state,
			byte[] buffer, int offset, int count, bool optional,
			CancellationToken token)
		where TStreamReader: IStreamReader<TStreamState>
	{
		var progress = 0;
		while (count > 0)
		{
			(state, var read) = await stream
				.ReadAsync(state, buffer, offset + progress, count, token)
				.Weave();

			if (read == 0)
				return progress == 0 && optional
					? ReadResult.Create(state, 0)
					: throw EndOfStream();

			progress += read;
			count -= read;
		}

		return ReadResult.Create(state, progress);
	}
}
