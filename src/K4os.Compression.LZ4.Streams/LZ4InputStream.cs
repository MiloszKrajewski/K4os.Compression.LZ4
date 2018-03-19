using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace K4os.Compression.LZ4.Streams
{
	public class LZ4InputStream: Stream
	{
		public LZ4InputStream(Stream inner)
		{
			throw new NotImplementedException();
		}

		public override void Flush()
		{
			throw new NotImplementedException();
		}

		public override int Read(byte[] buffer, int offset, int count)
		{
			throw new NotImplementedException();
		}

		public override long Seek(long offset, SeekOrigin origin)
		{
			throw new NotImplementedException();
		}

		public override void SetLength(long value)
		{
			throw new NotImplementedException();
		}

		public override void Write(byte[] buffer, int offset, int count)
		{
			throw new NotImplementedException();
		}

		public override int ReadByte()
		{
			return base.ReadByte();
		}

		public override void WriteByte(byte value)
		{
			base.WriteByte(value);
		}

		public override bool CanRead { get; }
		public override bool CanSeek { get; }
		public override bool CanWrite { get; }
		public override long Length { get; }
		public override long Position { get; set; }
		public override bool CanTimeout { get; }
		public override int WriteTimeout { get; set; }
		public override int ReadTimeout { get; set; }

		public override Task FlushAsync(CancellationToken cancellationToken)
		{
			return base.FlushAsync(cancellationToken);
		}

		public override Task<int> ReadAsync(
			byte[] buffer, int offset, int count, CancellationToken cancellationToken)
		{
			return base.ReadAsync(buffer, offset, count, cancellationToken);
		}

		public override Task WriteAsync(
			byte[] buffer, int offset, int count, CancellationToken cancellationToken)
		{
			return base.WriteAsync(buffer, offset, count, cancellationToken);
		}

		protected override void Dispose(bool disposing)
		{
			base.Dispose(disposing);
		}
	}
}
