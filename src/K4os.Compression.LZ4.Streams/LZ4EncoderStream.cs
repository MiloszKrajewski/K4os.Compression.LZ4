using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using K4os.Compression.LZ4.Encoders;
using K4os.Hash.xxHash;

namespace K4os.Compression.LZ4.Streams
{
	/// <summary>
	/// LZ4 compression stream. 
	/// </summary>
	public partial class LZ4EncoderStream: Stream, IDisposable
	{
		private readonly Stream _inner;

		private ILZ4Encoder _encoder;
		private readonly Func<ILZ4Descriptor, ILZ4Encoder> _encoderFactory;

		private readonly ILZ4Descriptor _descriptor;
		private readonly bool _leaveOpen;

		private byte[] _buffer;
		private long _position;

		/// <summary>Creates new instance of <see cref="LZ4EncoderStream"/>.</summary>
		/// <param name="inner">Inner stream.</param>
		/// <param name="descriptor">LZ4 Descriptor.</param>
		/// <param name="encoderFactory">Function which will take descriptor and return
		/// appropriate encoder.</param>
		/// <param name="leaveOpen">Indicates if <paramref name="inner"/> stream should be left
		/// open after disposing.</param>
		public LZ4EncoderStream(
			Stream inner,
			ILZ4Descriptor descriptor,
			Func<ILZ4Descriptor, ILZ4Encoder> encoderFactory,
			bool leaveOpen = false)
		{
			_inner = inner;
			_descriptor = descriptor;
			_encoderFactory = encoderFactory;
			_leaveOpen = leaveOpen;
		}

		/// <inheritdoc />
		public override void Flush() =>
			_inner.Flush();

		/// <inheritdoc />
		public override Task FlushAsync(CancellationToken cancellationToken) =>
			_inner.FlushAsync(cancellationToken);

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
			Write(_buffer16, _length16, 1);
		}

		/// <inheritdoc />
		public override void Write(byte[] buffer, int offset, int count) =>
			WriteImpl(buffer.AsSpan(offset, count));

		/// <inheritdoc />
		public override async Task WriteAsync(
			byte[] buffer, int offset, int count, CancellationToken cancellationToken) =>
			await WriteImplAsync(buffer.AsMemory(offset, count), cancellationToken);

		#if NETSTANDARD2_1

		/// <inheritdoc />
		public override void Write(ReadOnlySpan<byte> buffer) =>
			WriteImpl(buffer);

		/// <inheritdoc />
		public override async ValueTask WriteAsync(
			ReadOnlyMemory<byte> buffer,
			CancellationToken cancellationToken = new CancellationToken()) =>
			await WriteImplAsync(buffer, cancellationToken);

		#endif

		private void WriteImpl(ReadOnlySpan<byte> buffer)
		{
			if (TryStashFrame())
				FlushStash();

			var offset = 0;
			var count = buffer.Length;

			while (count > 0)
				WriteBlock(
					TopupAndEncode(buffer, ref offset, ref count));
		}

		private async Task WriteImplAsync(ReadOnlyMemory<byte> buffer, CancellationToken token)
		{
			if (TryStashFrame())
				await FlushStashAsync(token);

			var offset = 0;
			var count = buffer.Length;

			while (count > 0)
				await WriteBlockAsync(
					TopupAndEncode(buffer.Span, ref offset, ref count), token);
		}

		internal readonly struct BlockInfo
		{
			private readonly byte[] _buffer;
			private readonly int _length;

			public byte[] Buffer => _buffer;
			public int Offset => 0;
			public int Length => Math.Abs(_length);
			public bool Compressed => _length > 0;
			public bool Ready => _length != 0;

			public BlockInfo(byte[] buffer, EncoderAction action, int length)
			{
				_buffer = buffer;
				_length = action switch {
					EncoderAction.Encoded => length,
					EncoderAction.Copied => -length,
					_ => 0,
				};
			}
		}

		[SuppressMessage("ReSharper", "InconsistentNaming")]
		private bool TryStashFrame()
		{
			if (_encoder != null)
				return _index16 > 0;
			
			Stash4(0x184D2204);

			var headerIndex = _index16;

			const int versionCode = 0x01;
			var blockChaining = _descriptor.Chaining;
			var blockChecksum = _descriptor.BlockChecksum;
			var contentChecksum = _descriptor.ContentChecksum;
			var hasContentSize = _descriptor.ContentLength.HasValue;
			var hasDictionary = _descriptor.Dictionary.HasValue;

			var FLG =
				(versionCode << 6) |
				((blockChaining ? 0 : 1) << 5) |
				((blockChecksum ? 1 : 0) << 4) |
				((hasContentSize ? 1 : 0) << 3) |
				((contentChecksum ? 1 : 0) << 2) |
				(hasDictionary ? 1 : 0);

			var blockSize = _descriptor.BlockSize;

			var BD = MaxBlockSizeCode(blockSize) << 4;

			Stash2((ushort) ((FLG & 0xFF) | (BD & 0xFF) << 8));

			if (hasContentSize)
				throw NotImplemented(
					"ContentSize feature is not implemented"); // Stash8(contentSize);

			if (hasDictionary)
				throw NotImplemented(
					"Predefined dictionaries feature is not implemented"); // Stash4(dictionaryId);

			var headerDigest = XXH32.DigestOf(
				_buffer16, headerIndex, _index16 - headerIndex);
			var HC = (byte) (headerDigest >> 8);

			Stash1(HC);

			_encoder = CreateEncoder();
			_buffer = new byte[LZ4Codec.MaximumOutputSize(blockSize)];
			
			return _index16 > 0;
		}

		private ILZ4Encoder CreateEncoder()
		{
			var encoder = _encoderFactory(_descriptor);
			if (encoder.BlockSize > _descriptor.BlockSize)
				throw InvalidValue("BlockSize is greater than declared");

			return encoder;
		}

		private BlockInfo TopupAndEncode(
			ReadOnlySpan<byte> buffer, ref int offset, ref int count)
		{
			var action = _encoder.TopupAndEncode(
				buffer.Slice(offset, count),
				_buffer.AsSpan(),
				false, true,
				out var loaded,
				out var encoded);

			_position += loaded;
			offset += loaded;
			count -= loaded;

			return new BlockInfo(_buffer, action, encoded);
		}

		private BlockInfo FlushAndEncode()
		{
			var action = _encoder.FlushAndEncode(
				_buffer.AsSpan(), true, out var encoded);

			try
			{
				_encoder.Dispose();

				return new BlockInfo(_buffer, action, encoded);
			}
			finally
			{
				_encoder = null;
				_buffer = null;
			}
		}

		private void WriteBlock(BlockInfo block)
		{
			if (!block.Ready) return;

			StashBlockLength(block);
			FlushStash();

			InnerWrite(block);

			StashBlockChecksum(block);
			FlushStash();
		}

		private async Task WriteBlockAsync(
			BlockInfo block, CancellationToken token = default)
		{
			if (!block.Ready) return;

			StashBlockLength(block);
			await FlushStashAsync(token);

			await InnerWriteAsync(block, token);

			StashBlockChecksum(block);
			await FlushStashAsync(token);
		}

		private void CloseFrame()
		{
			if (_encoder == null)
				return;

			WriteBlock(FlushAndEncode());

			StashStreamEnd();
			FlushStash();
		}

		private async Task CloseFrameAsync(CancellationToken token = default)
		{
			if (_encoder == null)
				return;

			await WriteBlockAsync(FlushAndEncode(), token);

			StashStreamEnd();
			await FlushStashAsync(token);
		}

		private void StashBlockLength(BlockInfo block) =>
			Stash4((uint) block.Length | (block.Compressed ? 0 : 0x80000000));

		// ReSharper disable once UnusedParameter.Local
		private void StashBlockChecksum(BlockInfo block)
		{
			// NOTE: block will carry checksum one day

			if (_descriptor.BlockChecksum)
				throw NotImplemented("BlockChecksum");
		}

		private void StashStreamEnd()
		{
			Stash4(0);

			if (_descriptor.ContentChecksum)
				throw NotImplemented("ContentChecksum");
		}

		/// <inheritdoc />
		public new void Dispose()
		{
			Dispose(true);
			base.Dispose();
		}

		/// <inheritdoc />
		protected override void Dispose(bool disposing)
		{
			if (disposing)
			{
				CloseFrame();
				if (!_leaveOpen)
					_inner.Dispose();
			}

			base.Dispose(disposing);
		}

		#if NETSTANDARD2_1

		/// <inheritdoc />
		public override async ValueTask DisposeAsync()
		{
			await DisposeAsync(true);
			await base.DisposeAsync();
		}

		/// <summary>Performs dispose action. When inheriting remember to call base.</summary>
		/// <param name="disposing"><c>true</c> if it got called because of explicit dispose call
		/// not garbage collection.</param>
		/// <returns>Task when finished.</returns>
		protected async Task DisposeAsync(bool disposing)
		{
			if (disposing)
			{
				await CloseFrameAsync();
				if (!_leaveOpen)
					await _inner.DisposeAsync();
			}

			base.Dispose(disposing);
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
