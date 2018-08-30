using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using K4os.Compression.LZ4.Encoders;
using K4os.Hash.xxHash;

namespace K4os.Compression.LZ4.Streams
{
	public class LZ4OutputStream: Stream
	{
		private readonly Stream _inner;
		private readonly byte[] _buffer16 = new byte[16];
		private int _index16;

		private ILZ4StreamEncoder _encoder;
		private readonly Func<ILZ4FrameInfo, ILZ4StreamEncoder> _encoderFactory;

		private readonly ILZ4FrameInfo _frameInfo;
		private byte[] _buffer;

		public LZ4OutputStream(
			Stream inner, ILZ4FrameInfo frameInfo,
			Func<ILZ4FrameInfo, ILZ4StreamEncoder> encoderFactory)
		{
			_inner = inner;
			_frameInfo = frameInfo;
			_encoderFactory = encoderFactory;
		}

		public override void Flush() => _inner.Flush();

		public override Task FlushAsync(CancellationToken cancellationToken) =>
			_inner.FlushAsync(cancellationToken);

		public void Close() { CloseFrame(); }

		public override void WriteByte(byte value)
		{
			_buffer16[_index16] = value;
			Write(_buffer16, _index16, 1);
		}

		public override void Write(byte[] buffer, int offset, int count)
		{
			while (count > 0)
			{
				if (_encoder == null)
					WriteFrame();

				var action = _encoder.TopupAndEncode(
					buffer, offset, count,
					_buffer, 0, _buffer.Length,
					false,
					out var loaded,
					out var encoded);

				switch (action)
				{
					case EncoderAction.None:
					case EncoderAction.Loaded:
						break;
					case EncoderAction.Copied:
						WriteBlock(encoded, false);
						break;
					case EncoderAction.Encoded:
						WriteBlock(encoded, true);
						break;
				}

				offset += loaded;
				count -= loaded;
			}
		}

		[SuppressMessage("ReSharper", "InconsistentNaming")]
		[SuppressMessage("ReSharper", "UnreachableCode")]
		private void WriteFrame()
		{
			Write32(0x184D2204);
			Flush16();

			Flush16();

			const int version = 0x01;
			var chaining = _frameInfo.Chaining;
			var bchecksum = _frameInfo.BlockChecksum;
			var cchecksum = _frameInfo.ContentChecksum;

			var FLG =
				(version << 6) |
				((chaining ? 0 : 1) << 5) |
				((bchecksum ? 1 : 0) << 4) |
				((cchecksum ? 1 : 0) << 2);

			const bool hasContentSize = false;
			const bool hasDictionary = false;
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

			_encoder = _encoderFactory(_frameInfo);
			_buffer = new byte[blockSize];
		}

		public void CloseFrame()
		{
			if (_encoder.BytesReady > 0)
			{
				#error after Copy bytes are still ready 

				var encoded = _encoder.Encode(_buffer, 0, _buffer.Length);
				if (encoded > 0)
				{
					WriteBlock(encoded, true);
				}
				else
				{
					var copied = _encoder.Copy(_buffer, 0, _buffer.Length);
					WriteBlock(copied, false);
				}
			}

			Write32(0);
			Flush16();

			if (_frameInfo.ContentChecksum)
				throw NotImplemented("ContentChecksum");

			_encoder = null;
			_buffer = null;
		}

		private int MaxBlockSizeCode(int blockSize)
		{
			if (blockSize <= Mem.K64)
				return 4;

			if (blockSize <= Mem.K256)
				return 5;

			if (blockSize <= Mem.M1)
				return 6;

			if (blockSize <= Mem.M4)
				return 7;

			throw InvalidBlockSize(blockSize);
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

		protected override void Dispose(bool disposing)
		{
			base.Dispose(disposing);
			if (disposing)
				Close();
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

		private ArgumentException InvalidBlockSize(int blockSize) =>
			new ArgumentException($"Invalid block size ${blockSize} for {GetType().Name}");
	}
}
