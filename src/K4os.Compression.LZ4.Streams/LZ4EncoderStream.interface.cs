using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace K4os.Compression.LZ4.Streams
{
	/// <summary>
	/// LZ4 compression stream. 
	/// </summary>
	public partial class LZ4EncoderStream
	{
		/// <inheritdoc />
		public override void Flush() =>
			InnerFlush();

		/// <inheritdoc />
		public override Task FlushAsync(CancellationToken cancellationToken) =>
			InnerFlushAsync(cancellationToken);

		#if NETSTANDARD1_6
		/// <summary>Closes stream.</summary>
		public void Close() { CloseFrame(); }
		#else
		/// <inheritdoc />
		public override void Close()
		{
			CloseFrame();
			base.Close();
		}
		#endif

		/// <inheritdoc />
		public override void WriteByte(byte value)
		{
			_buffer16[_length16] = value;
			WriteImpl(_buffer16.AsSpan(_length16, 1));
		}

		/// <inheritdoc />
		public override void Write(byte[] buffer, int offset, int count) =>
			WriteImpl(buffer.AsSpan(offset, count));

		/// <inheritdoc />
		public override async Task WriteAsync(
			byte[] buffer, int offset, int count, CancellationToken cancellationToken) =>
			await WriteImplAsync(cancellationToken, buffer.AsMemory(offset, count));

		#if NETSTANDARD2_1

		/// <inheritdoc />
		public override void Write(ReadOnlySpan<byte> buffer) =>
			WriteImpl(buffer);

		/// <inheritdoc />
		public override async ValueTask WriteAsync(
			ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default) =>
			await WriteImplAsync(cancellationToken, buffer);

		#endif

		/// <inheritdoc />
		protected override void Dispose(bool disposing)
		{
			if (disposing) InnerDispose();

			base.Dispose(disposing);
		}

		#if NETSTANDARD2_1

		/// <inheritdoc />
		public override async ValueTask DisposeAsync()
		{
			await InnerDisposeAsync(CancellationToken.None);
			await base.DisposeAsync();
		}

		#endif

		/// <inheritdoc />
		public override bool CanRead => false;

		/// <inheritdoc />
		public override bool CanSeek => false;

		/// <inheritdoc />
		public override bool CanWrite => _inner.CanWrite;

		/// <summary>Length of the stream and number of bytes written so far.</summary>
		public override long Length => _position;

		/// <summary>Read-only position in the stream. Trying to set it will throw
		/// <see cref="InvalidOperationException"/>.</summary>
		public override long Position
		{
			get => _position;
			set => throw InvalidOperation("Position");
		}

		/// <inheritdoc />
		public override bool CanTimeout => _inner.CanTimeout;

		/// <inheritdoc />
		public override int ReadTimeout
		{
			get => _inner.ReadTimeout;
			set => _inner.ReadTimeout = value;
		}

		/// <inheritdoc />
		public override int WriteTimeout
		{
			get => _inner.WriteTimeout;
			set => _inner.WriteTimeout = value;
		}

		/// <inheritdoc />
		public override long Seek(long offset, SeekOrigin origin) =>
			throw InvalidOperation("Seek");

		/// <inheritdoc />
		public override void SetLength(long value) =>
			throw InvalidOperation("SetLength");

		/// <inheritdoc />
		public override int Read(byte[] buffer, int offset, int count) =>
			throw InvalidOperation("Read");

		/// <inheritdoc />
		public override Task<int> ReadAsync(
			byte[] buffer, int offset, int count, CancellationToken cancellationToken) =>
			throw InvalidOperation("ReadAsync");

		#if NETSTANDARD2_1

		/// <inheritdoc />
		public override ValueTask<int> ReadAsync(
			Memory<byte> buffer, CancellationToken cancellationToken = default) =>
			throw InvalidOperation("ReadAsync");

		#endif

		/// <inheritdoc />
		public override int ReadByte() => throw InvalidOperation("ReadByte");

		private NotImplementedException NotImplemented(string operation) =>
			new NotImplementedException(
				$"Feature {operation} has not been implemented in {GetType().Name}");

		private InvalidOperationException InvalidOperation(string operation) =>
			new InvalidOperationException(
				$"Operation {operation} is not allowed for {GetType().Name}");

		private static ArgumentException InvalidValue(string description) =>
			new ArgumentException(description);

		private ArgumentException InvalidBlockSize(int blockSize) =>
			new ArgumentException($"Invalid block size ${blockSize} for {GetType().Name}");
	}
}
