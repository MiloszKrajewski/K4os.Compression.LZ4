using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using K4os.Compression.LZ4.Streams.Internal;

namespace K4os.Compression.LZ4.Streams.NewStreams
{
	public interface IStreamReader
	{
		int Read(byte[] buffer, int offset, int length);
		Task<int> ReadAsync(byte[] buffer, int offset, int length, CancellationToken token);
	}

	public interface IStreamWriter
	{
		void Write(byte[] buffer, int offset, int length);
		Task WriteAsync(byte[] buffer, int offset, int length, CancellationToken token);
	}

	public static class StreamAdapterExtensions
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

	public readonly struct StreamAdapter: IStreamReader, IStreamWriter
	{
		private readonly Stream _stream;

		public StreamAdapter(Stream stream) => _stream = stream;

		public int Read(
			byte[] buffer, int offset, int length) =>
			_stream.Read(buffer, offset, length);

		public Task<int> ReadAsync(
			byte[] buffer, int offset, int length, CancellationToken token) =>
			_stream.ReadAsync(buffer, offset, length, token);

		public void Write(
			byte[] buffer, int offset, int length) =>
			_stream.Write(buffer, offset, length);

		public Task WriteAsync(
			byte[] buffer, int offset, int length, CancellationToken token) =>
			_stream.WriteAsync(buffer, offset, length, token);
	}
}
