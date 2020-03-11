using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using K4os.Compression.LZ4.Encoders;
using K4os.Compression.LZ4.Internal;
using K4os.Hash.xxHash;

namespace K4os.Compression.LZ4.Streams
{
	/// <summary>
	/// LZ4 compression stream. 
	/// </summary>
	public class LZ4EncoderStream: Stream, IDisposable
	{
		private readonly Stream _inner;
		
		// ReSharper disable once InconsistentNaming
		private const int _length16 = 16;
		private readonly byte[] _buffer16 = new byte[_length16 + 8];
		private int _index16;

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
		public override void Flush() => _inner.Flush();

		/// <inheritdoc />
		public override Task FlushAsync(CancellationToken cancellationToken) =>
			_inner.FlushAsync(cancellationToken);

		#if NETSTANDARD1_6
		/// <summary>Closes stream.</summary>
		public void Close() { CloseFrame(); }
		#else
		/// <inheritdoc />
		public override void Close() { CloseFrame(); }
		#endif

		/// <inheritdoc />
		public override void WriteByte(byte value)
		{
			_buffer16[_length16] = value;
			Write(_buffer16, _length16, 1);
		}

		/// <inheritdoc />
		public override void Write(byte[] buffer, int offset, int count)
		{
			if (_encoder == null)
				WriteFrame();

			while (count > 0)
			{
				var action = _encoder.TopupAndEncode(
					buffer, offset, count,
					_buffer, 0, _buffer.Length,
					false, true,
					out var loaded,
					out var encoded);
				WriteBlock(encoded, action);
				
				_position += loaded;
				
				offset += loaded;
				count -= loaded;
			}
		}

		[SuppressMessage("ReSharper", "InconsistentNaming")]
		private void WriteFrame()
		{
			Stash32(0x184D2204);
			FlushStash();

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

			Stash16((ushort) ((FLG & 0xFF) | (BD & 0xFF) << 8));

			if (hasContentSize)
				throw NotImplemented(
					"ContentSize feature is not implemented"); // Write64(contentSize);

			if (hasDictionary)
				throw NotImplemented(
					"Predefined dictionaries feature is not implemented"); // Write32(dictionaryId);

			var HC = (byte) (XXH32.DigestOf(_buffer16, 0, _index16) >> 8);

			Stash8(HC);
			FlushStash();

			_encoder = CreateEncoder();
			_buffer = new byte[LZ4Codec.MaximumOutputSize(blockSize)];
		}

		private ILZ4Encoder CreateEncoder()
		{
			var encoder = _encoderFactory(_descriptor);
			if (encoder.BlockSize > _descriptor.BlockSize)
				throw InvalidValue("BlockSize is greater than declared");

			return encoder;
		}

		private void CloseFrame()
		{
			if (_encoder == null)
				return;

			try
			{
				var action = _encoder.FlushAndEncode(
					_buffer, 0, _buffer.Length, true, out var encoded);
				WriteBlock(encoded, action);

				Stash32(0);
				FlushStash();

				if (_descriptor.ContentChecksum)
					throw NotImplemented("ContentChecksum");

				_buffer = null;

				_encoder.Dispose();
			}
			finally
			{
				_encoder = null;
			}
		}

		private int MaxBlockSizeCode(int blockSize) =>
			blockSize <= Mem.K64 ? 4 :
			blockSize <= Mem.K256 ? 5 :
			blockSize <= Mem.M1 ? 6 :
			blockSize <= Mem.M4 ? 7 :
			throw InvalidBlockSize(blockSize);

		private void WriteBlock(int length, EncoderAction action)
		{
			switch (action)
			{
				case EncoderAction.Copied:
					WriteBlock(length, false);
					break;
				case EncoderAction.Encoded:
					WriteBlock(length, true);
					break;
			}
		}

		private void WriteBlock(int length, bool compressed)
		{
			if (length <= 0)
				return;

			Stash32((uint) length | (compressed ? 0 : 0x80000000));
			FlushStash();

			_inner.Write(_buffer, 0, length);

			if (_descriptor.BlockChecksum)
				throw NotImplemented("BlockChecksum");
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
			base.Dispose(disposing);
			if (!disposing)
				return;

			CloseFrame();
			if (!_leaveOpen)
				_inner.Dispose();
		}

		private void Stash8(byte value) { _buffer16[_index16++] = value; }

		private void Stash16(ushort value)
		{
			_buffer16[_index16 + 0] = (byte) value;
			_buffer16[_index16 + 1] = (byte) (value >> 8);
			_index16 += 2;
		}

		private void Stash32(uint value)
		{
			_buffer16[_index16 + 0] = (byte) value;
			_buffer16[_index16 + 1] = (byte) (value >> 8);
			_buffer16[_index16 + 2] = (byte) (value >> 16);
			_buffer16[_index16 + 3] = (byte) (value >> 24);
			_index16 += 4;
		}

		/*
		private void Stash64(ulong value)
		{
		    _buffer16[_index16 + 0] = (byte) value;
		    _buffer16[_index16 + 1] = (byte) (value >> 8);
		    _buffer16[_index16 + 2] = (byte) (value >> 16);
		    _buffer16[_index16 + 3] = (byte) (value >> 24);
		    _buffer16[_index16 + 4] = (byte) (value >> 32);
		    _buffer16[_index16 + 5] = (byte) (value >> 40);
		    _buffer16[_index16 + 6] = (byte) (value >> 48);
		    _buffer16[_index16 + 7] = (byte) (value >> 56);
		    _index16 += 8;
		}
		*/

		private void FlushStash()
		{
			if (_index16 > 0)
				_inner.Write(_buffer16, 0, _index16);
			_index16 = 0;
		}

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
