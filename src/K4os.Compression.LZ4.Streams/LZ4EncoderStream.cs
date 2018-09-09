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
	public class LZ4EncoderStream: Stream, IDisposable
	{
		private readonly Stream _inner;
		private readonly byte[] _buffer16 = new byte[16];
		private int _index16;

		private ILZ4Encoder _encoder;
		private readonly Func<ILZ4FrameInfo, ILZ4Encoder> _encoderFactory;

		private readonly ILZ4FrameInfo _frameInfo;
		private byte[] _buffer;

		public LZ4EncoderStream(
			Stream inner, ILZ4FrameInfo frameInfo,
			Func<ILZ4FrameInfo, ILZ4Encoder> encoderFactory)
		{
			_inner = inner;
			_frameInfo = frameInfo;
			_encoderFactory = encoderFactory;
		}

		public override void Flush() => _inner.Flush();

		public override Task FlushAsync(CancellationToken cancellationToken) =>
			_inner.FlushAsync(cancellationToken);

		#if NET46 || NETSTANDARD2_0
		public override void Close() { CloseFrame(); }
		#else
		public void Close() { CloseFrame(); }
		#endif

		public override void WriteByte(byte value)
		{
			_buffer16[_index16] = value;
			Write(_buffer16, _index16, 1);
		}

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

				offset += loaded;
				count -= loaded;
			}
		}

		[SuppressMessage("ReSharper", "InconsistentNaming")]
		private void WriteFrame()
		{
			Write32(0x184D2204);
			Flush16();

			const int versionCode = 0x01;
			var blockChaining = _frameInfo.Chaining;
			var blockChecksum = _frameInfo.BlockChecksum;
			var contentChecksum = _frameInfo.ContentChecksum;

			var FLG =
				(versionCode << 6) |
				((blockChaining ? 0 : 1) << 5) |
				((blockChecksum ? 1 : 0) << 4) |
				((contentChecksum ? 1 : 0) << 2);

			var hasContentSize = _frameInfo.ContentLength.HasValue;
			var hasDictionary = _frameInfo.Dictionary.HasValue;
			var blockSize = _frameInfo.BlockSize;

			var BD =
				((hasContentSize ? 1 : 0) << 3) |
				((hasDictionary ? 1 : 0) << 0) |
				(MaxBlockSizeCode(blockSize) << 4);

			Write16((ushort) ((FLG & 0xFF) | (BD & 0xFF) << 8));

			if (hasContentSize)
				throw NotImplemented(
					"ContentSize feature is not implemented"); // Write64(contentSize);

			if (hasDictionary)
				throw NotImplemented(
					"Predefined dictionaries feature is not implemented"); // Write32(dictionaryId);

			var HC = (byte) (XXH32.DigestOf(_buffer16, 0, _index16) >> 8);

			Write8(HC);
			Flush16();

			_encoder = CreateEncoder();
			_buffer = new byte[LZ4Codec.MaximumOutputSize(blockSize)];
		}

		private ILZ4Encoder CreateEncoder()
		{
			var encoder = _encoderFactory(_frameInfo);
			if (encoder.BlockSize > _frameInfo.BlockSize)
				throw InvalidValue("BlockSize is greater than declared");

			return encoder;
		}

		public void CloseFrame()
		{
			if (_encoder == null)
				return;

			try
			{
				var action = _encoder.FlushAndEncode(_buffer, 0, _buffer.Length, true, out var encoded);
				WriteBlock(encoded, action);

				Write32(0);
				Flush16();
				
				if (_frameInfo.ContentChecksum)
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

			Write32((uint) length | (compressed ? 0 : 0x80000000));
			Flush16();

			_inner.Write(_buffer, 0, length);

			if (_frameInfo.BlockChecksum)
				throw NotImplemented("BlockChecksum");
		}

		public new void Dispose()
		{
			Dispose(true);
			base.Dispose();
		}

		protected override void Dispose(bool disposing)
		{
			base.Dispose(disposing);
			if (!disposing)
				return;

			CloseFrame();
			_inner.Dispose();
		}

		private void Write8(byte value) { _buffer16[_index16++] = value; }

		private void Write16(ushort value)
		{
			_buffer16[_index16 + 0] = (byte) value;
			_buffer16[_index16 + 1] = (byte) (value >> 8);
			_index16 += 2;
		}

		private void Write32(uint value)
		{
			_buffer16[_index16 + 0] = (byte) value;
			_buffer16[_index16 + 1] = (byte) (value >> 8);
			_buffer16[_index16 + 2] = (byte) (value >> 16);
			_buffer16[_index16 + 3] = (byte) (value >> 24);
			_index16 += 4;
		}

		/*
		private void Write64(ulong value)
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

		private void Flush16()
		{
			if (_index16 > 0)
				_inner.Write(_buffer16, 0, _index16);
			_index16 = 0;
		}

		public override bool CanRead => false;
		public override bool CanSeek => false;
		public override bool CanWrite => _inner.CanWrite;
		public override long Length => -1;

		public override long Position
		{
			get => -1;
			set => throw InvalidOperation("Position");
		}

		public override bool CanTimeout => _inner.CanTimeout;

		public override int ReadTimeout
		{
			get => _inner.ReadTimeout;
			set => _inner.ReadTimeout = value;
		}

		public override int WriteTimeout
		{
			get => _inner.WriteTimeout;
			set => _inner.WriteTimeout = value;
		}

		public override long Seek(long offset, SeekOrigin origin) =>
			throw InvalidOperation("Seek");

		public override void SetLength(long value) =>
			throw InvalidOperation("SetLength");

		public override int Read(byte[] buffer, int offset, int count) =>
			throw InvalidOperation("Read");

		public override Task<int> ReadAsync(
			byte[] buffer, int offset, int count, CancellationToken cancellationToken) =>
			throw InvalidOperation("ReadAsync");

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
