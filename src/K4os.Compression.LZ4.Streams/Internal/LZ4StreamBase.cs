using System;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using K4os.Hash.xxHash;

namespace K4os.Compression.LZ4.Streams.Internal
{
	/// <summary>
	/// Base class for LZ4 encoder and decoder streams.
	/// Cannot be used on its own it just provides some shared functionality.
	/// </summary>
	public abstract class LZ4StreamBase: Stream
	{
		private readonly Stream _inner;
		private readonly bool _leaveOpen;
		private Stash _stash;

		private protected LZ4StreamBase(Stream inner, bool leaveOpen)
		{
			_inner = inner;
			_leaveOpen = leaveOpen;
			_stash = new Stash(_inner, 16);
		}

		private protected ref Stash Stash => ref _stash;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private protected uint DigestOfStash(int offset = 0) =>
			XXH32.DigestOf(Stash.AsSpan(offset));

		private protected NotImplementedException NotImplemented(string operation) =>
			new NotImplementedException(
				$"Feature {operation} has not been implemented in {GetType().Name}");

		private protected InvalidOperationException InvalidOperation(string operation) =>
			new InvalidOperationException(
				$"Operation {operation} is not allowed for {GetType().Name}");

		private protected static ArgumentException InvalidValue(string description) =>
			new ArgumentException(description);

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private protected void InnerFlush(in EmptyToken _) =>
			_inner.Flush();

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private protected Task InnerFlush(in CancellationToken token) =>
			_inner.FlushAsync(token);

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private protected void InnerWriteBlock(
			in EmptyToken _, byte[] buffer, int offset, int length) =>
			_inner.Write(buffer, offset, length);

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private protected Task InnerWriteBlock(
			in CancellationToken token, byte[] buffer, int offset, int length) =>
			_inner.WriteAsync(buffer, offset, length, token);

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private protected int InnerReadBlock(
			in EmptyToken _, byte[] buffer, int offset, int length) =>
			_inner.TryReadBlock(buffer, offset, length, false);

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private protected Task<int> InnerReadBlock(
			in CancellationToken token, byte[] buffer, int offset, int length) =>
			_inner.TryReadBlockAsync(buffer, offset, length, false, token);

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private protected void InnerDispose(in EmptyToken _, bool force)
		{
			if (force || !_leaveOpen) _inner.Dispose();
		}

		#if NETSTANDARD2_1

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private protected ValueTask InnerDispose(in CancellationToken _, bool force) => 
			force || !_leaveOpen ? _inner.DisposeAsync() : new ValueTask();

		#endif

		/// <inheritdoc />
		public override bool CanRead => _inner.CanRead;

		/// <inheritdoc />
		public override bool CanWrite => _inner.CanWrite;

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
		public override bool CanSeek => false;

		/// <summary>Read-only position in the stream. Trying to set it will throw
		/// <see cref="InvalidOperationException"/>.</summary>
		public override long Position
		{
			set => Seek(value, SeekOrigin.Begin);
		}

		/// <inheritdoc />
		public override long Seek(long offset, SeekOrigin origin) =>
			throw InvalidOperation("Seek");

		/// <inheritdoc />
		public override void SetLength(long value) =>
			throw InvalidOperation("SetLength");

		/// <inheritdoc />
		public override int ReadByte() =>
			throw InvalidOperation("ReadByte");

		/// <inheritdoc />
		public override int Read(byte[] buffer, int offset, int count) =>
			throw InvalidOperation("Read");

		/// <inheritdoc />
		public override Task<int> ReadAsync(
			byte[] buffer, int offset, int count, CancellationToken cancellationToken) =>
			throw InvalidOperation("ReadAsync");

		#if NETSTANDARD2_1

		/// <inheritdoc />
		public override int Read(Span<byte> buffer) => 
			throw InvalidOperation("Read");

		/// <inheritdoc />
		public override ValueTask<int> ReadAsync(
			Memory<byte> buffer, CancellationToken cancellationToken = default) =>
			throw InvalidOperation("ReadAsync");

		#endif

		/// <inheritdoc />
		public override void WriteByte(byte value) =>
			throw InvalidOperation("WriteByte");

		/// <inheritdoc />
		public override void Write(byte[] buffer, int offset, int count) =>
			throw InvalidOperation("Write");

		/// <inheritdoc />
		public override Task WriteAsync(
			byte[] buffer, int offset, int count, CancellationToken cancellationToken) =>
			throw InvalidOperation("WriteAsync");

		#if NETSTANDARD2_1

		/// <inheritdoc />
		public override void Write(ReadOnlySpan<byte> buffer) => 
			throw InvalidOperation("Write");

		/// <inheritdoc />
		public override ValueTask WriteAsync(
			ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default) =>
			throw InvalidOperation("WriteAsync");

		#endif
	}
}
