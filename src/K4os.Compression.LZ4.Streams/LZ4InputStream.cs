using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace K4os.Compression.LZ4.Streams
{
	public class LZ4InputStream: Stream
	{
		private readonly Stream _inner;

		public LZ4InputStream(Stream inner)
		{
			_inner = inner;
		}

		public override void Flush() => 
			_inner.Flush();

		public override Task FlushAsync(CancellationToken cancellationToken) =>
			_inner.FlushAsync(cancellationToken);

		public override int Read(byte[] buffer, int offset, int count)
		{
			throw new NotImplementedException();
		}

		public override Task<int> ReadAsync(
			byte[] buffer, int offset, int count, CancellationToken cancellationToken)
		{
			throw new NotImplementedException();
		}

		public override int ReadByte()
		{
			throw new NotImplementedException();
		}

		protected override void Dispose(bool disposing)
		{
			if (!disposing)
				return;

			_inner.Dispose();
		}

		public override bool CanRead => _inner.CanRead;
		public override bool CanSeek => false;
		public override bool CanWrite => false;
		public override long Length => -1;

		public override long Position
		{
			get => -1;
			set => throw new InvalidOperationException();
		}

		public override bool CanTimeout => _inner.CanTimeout;

		public override int WriteTimeout
		{
			get => _inner.WriteTimeout;
			set => _inner.WriteTimeout = value;
		}

		public override int ReadTimeout
		{
			get => _inner.ReadTimeout;
			set => _inner.ReadTimeout = value;
		}

		public override long Seek(long offset, SeekOrigin origin) =>
			throw InvalidOperation("Seek");

		public override void SetLength(long value) =>
			throw InvalidOperation("SetLength");

		public override void Write(byte[] buffer, int offset, int count) =>
			throw InvalidOperation("Write");

		public override void WriteByte(byte value) =>
			throw InvalidOperation("WriteByte");

		public override Task WriteAsync(
			byte[] buffer, int offset, int count, CancellationToken cancellationToken) =>
			throw InvalidOperation("WriteAsync");

		private Exception InvalidOperation(string operation) =>
			new InvalidOperationException($"Operation {operation} is not allowed for {GetType().Name}");
	}
}
