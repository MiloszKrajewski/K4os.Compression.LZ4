using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace K4os.Compression.LZ4.Streams
{
	public partial class LZ4DecoderStream
	{
		/// <inheritdoc />
		public override void Flush() =>
			InnerFlush();

		/// <inheritdoc />
		public override Task FlushAsync(CancellationToken cancellationToken) =>
			InnerFlush(cancellationToken);

		/// <inheritdoc />
		public override int Read(byte[] buffer, int offset, int count) => 
			ReadImpl(buffer, offset, count);

		/// <inheritdoc />
		public override int ReadByte() =>
			Read(_buffer16, _length16, 1) > 0 ? _buffer16[_length16] : -1;
		
		/// <inheritdoc />
		public new void Dispose()
		{
			Dispose(true);
			base.Dispose();
		}

		/// <inheritdoc />
		protected override void Dispose(bool disposing)
		{
			base.Dispose(disposing);
			if (!disposing)
				return;

			CloseFrame();
			if (!_leaveOpen)
				_inner.Dispose();
		}

		/// <inheritdoc />
		public override bool CanRead => _inner.CanRead;

		/// <inheritdoc />
		public override bool CanSeek => false;

		/// <inheritdoc />
		public override bool CanWrite => false;

		/// <summary>
		/// Length of stream. Please note, this will only work if original LZ4 stream has
		/// <c>ContentLength</c> field set in descriptor. Otherwise returned value will be <c>-1</c>.
		/// </summary>
		public override long Length
		{
			get
			{
				EnsureFrame();
				return _frameInfo?.ContentLength ?? -1;
			}
		}

		/// <summary>
		/// Position within the stream. Position can be read, but cannot be set as LZ4 stream does
		/// not have <c>Seek</c> capability.
		/// </summary>
		public override long Position
		{
			get => _position;
			set => throw InvalidOperation("SetPosition");
		}

		/// <inheritdoc />
		public override bool CanTimeout => _inner.CanTimeout;

		/// <inheritdoc />
		public override int WriteTimeout
		{
			get => _inner.WriteTimeout;
			set => _inner.WriteTimeout = value;
		}

		/// <inheritdoc />
		public override int ReadTimeout
		{
			get => _inner.ReadTimeout;
			set => _inner.ReadTimeout = value;
		}

		/// <inheritdoc />
		public override long Seek(long offset, SeekOrigin origin) =>
			throw InvalidOperation("Seek");

		/// <inheritdoc />
		public override void SetLength(long value) =>
			throw InvalidOperation("SetLength");

		/// <inheritdoc />
		public override void Write(byte[] buffer, int offset, int count) =>
			throw InvalidOperation("Write");

		/// <inheritdoc />
		public override void WriteByte(byte value) =>
			throw InvalidOperation("WriteByte");

		/// <inheritdoc />
		public override Task WriteAsync(
			byte[] buffer, int offset, int count, CancellationToken cancellationToken) =>
			throw InvalidOperation("WriteAsync");
	}
}
