using System;
using System.Threading;
using System.Threading.Tasks;
using K4os.Compression.LZ4.Streams.Internal;

namespace K4os.Compression.LZ4.Streams
{
	public partial class LZ4DecoderStream
	{
		/// <inheritdoc />
		public override void Flush() =>
			InnerFlush(EmptyToken.Value);

		/// <inheritdoc />
		public override Task FlushAsync(CancellationToken cancellationToken) =>
			InnerFlush(cancellationToken);
		
		/// <inheritdoc />
		public override int ReadByte() =>
			ReadImpl(EmptyToken.Value, Stash.OneByteSpan()) > 0 
				? Stash.OneByteValue() 
				: -1;

		/// <inheritdoc />
		public override int Read(byte[] buffer, int offset, int count) => 
			ReadImpl(EmptyToken.Value, buffer.AsSpan(offset, count));

		/// <inheritdoc />
		public override Task<int> ReadAsync(
			byte[] buffer, int offset, int count, CancellationToken cancellationToken) =>
			ReadImpl(cancellationToken, buffer.AsMemory(offset, count));
		
		#if NETSTANDARD2_1

		/// <inheritdoc />
		public override int Read(Span<byte> buffer) =>
			ReadImpl(EmptyToken.Value, buffer);

		/// <inheritdoc />
		public override ValueTask<int> ReadAsync(
			Memory<byte> buffer, CancellationToken cancellationToken = default) =>
			new ValueTask<int>(ReadImpl(cancellationToken, buffer));

		#endif

		/// <inheritdoc />
		protected override void Dispose(bool disposing)
		{
			if (disposing) DisposeImpl(EmptyToken.Value);
			base.Dispose(disposing);
		}

		#if NETSTANDARD2_1

		/// <inheritdoc />
		public override async ValueTask DisposeAsync()
		{
			await DisposeImpl(CancellationToken.None).Weave();
			await base.DisposeAsync().Weave();
		}

		#endif
		
		/// <inheritdoc />
		public override bool CanWrite => false;

		/// <summary>
		/// Length of stream. Please note, this will only work if original LZ4 stream has
		/// <c>ContentLength</c> field set in descriptor. Otherwise returned value will be <c>-1</c>.
		/// It will also require synchronous stream access,
		/// so it wont work if AllowSynchronousIO is false.
		/// </summary>
		public override long Length => GetLength(EmptyToken.Value);

		/// <summary>
		/// Position within the stream. Position can be read, but cannot be set as LZ4 stream does
		/// not have <c>Seek</c> capability.
		/// </summary>
		public override long Position => _position;
	}
}
