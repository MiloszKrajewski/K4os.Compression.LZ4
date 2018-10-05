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
	public class LZ4DecoderStream: Stream, IDisposable
	{
		private readonly Stream _inner;
		private readonly byte[] _buffer16 = new byte[16];
		private int _index16;

		private int _decoded;
		private readonly bool _interactive = true;

		private ILZ4Decoder _decoder;
		private readonly Func<ILZ4FrameInfo, ILZ4Decoder> _decoderFactory;

		private ILZ4FrameInfo _frameInfo;
		private byte[] _buffer;
		private readonly bool _leaveOpen;
		private long _position;

		public LZ4DecoderStream(
			Stream inner,
			Func<ILZ4FrameInfo, ILZ4Decoder> decoderFactory,
			bool leaveOpen = false)
		{
			_inner = inner;
			_decoderFactory = decoderFactory;
			_leaveOpen = leaveOpen;
			_position = 0;
		}

		public override void Flush() =>
			_inner.Flush();

		public override Task FlushAsync(CancellationToken cancellationToken) =>
			_inner.FlushAsync(cancellationToken);

		public override int Read(byte[] buffer, int offset, int count)
		{
			EnsureFrame();

			var read = 0;
			while (count > 0)
			{
				if (_decoded <= 0 && (_decoded = ReadBlock()) == 0)
					break;

				if (ReadDecoded(buffer, ref offset, ref count, ref read))
					break;
			}

			return read;
		}

		public override int ReadByte() =>
			Read(_buffer16, _index16, 1) > 0 ? _buffer16[_index16] : -1;
		
		private void EnsureFrame()
		{
			if (_decoder == null)
				ReadFrame();
		}

		[SuppressMessage("ReSharper", "InconsistentNaming")]
		private void ReadFrame()
		{
			Read0();

			var magic = TryRead32();
			if (magic != 0x184D2204)
				throw MagicNumberExpected();

			Read0();

			var FLG_BD = Read16();

			var FLG = FLG_BD & 0xFF;
			var BD = (FLG_BD >> 8) & 0xFF;

			var version = (FLG >> 6) & 0x11;

			if (version != 1)
				throw UnknownFrameVersion(version);
			
			var blockChaining = ((FLG >> 5) & 0x01) == 0;
			var blockChecksum = ((FLG >> 4) & 0x01) != 0;
			var hasContentSize = ((FLG >> 3) & 0x01) != 0;
			var contentChecksum = ((FLG >> 2) & 0x01) != 0;

			var blockSizeCode = (BD >> 4) & 0x07;

			var contentLength = hasContentSize ? (long?) Read64() : null;

			var actualHC = (byte) (XXH32.DigestOf(_buffer16, 0, _index16) >> 8);
			var expectedHC = Read8();

			if (actualHC != expectedHC)
				throw InvalidHeaderChecksum();

			var blockSize = MaxBlockSize(blockSizeCode);

			_frameInfo = new LZ4FrameInfo(contentLength, contentChecksum, blockChaining, blockChecksum, blockSize);
			_decoder = _decoderFactory(_frameInfo);
			_buffer = new byte[blockSize];
		}

		private void CloseFrame()
		{
			if (_decoder == null)
				return;

			try
			{
				_frameInfo = null;
				_buffer = null;

				// if you need any exceptions throw them here

				_decoder.Dispose();
			}
			finally
			{
				_decoder = null;
			}
		}

		private static int MaxBlockSize(int blockSizeCode)
		{
			switch (blockSizeCode)
			{
				case 7: return Mem.M4;
				case 6: return Mem.M1;
				case 5: return Mem.K256;
				case 4: return Mem.K64;
				default: return Mem.K64;
			}
		}

		private unsafe int ReadBlock()
		{
			Read0();

			var blockLength = (int) Read32();
			if (blockLength == 0)
			{
				if (_frameInfo.ContentChecksum)
					Read32();
				CloseFrame();
				return 0;
			}

			var uncompressed = (blockLength & 0x80000000) != 0;
			blockLength &= 0x7FFFFFFF;
			_inner.Read(_buffer, 0, blockLength);
			if (_frameInfo.BlockChecksum)
				Read32();

			fixed (byte* bufferP = _buffer)
				return uncompressed
					? _decoder.Inject(bufferP, blockLength)
					: _decoder.Decode(bufferP, blockLength);
		}

		private bool ReadDecoded(byte[] buffer, ref int offset, ref int count, ref int read)
		{
			if (_decoded <= 0)
				return true;

			var length = Math.Min(count, _decoded);
			_decoder.Drain(buffer, offset, -_decoded, length);
			_position += length;
			_decoded -= length;
			offset += length;
			count -= length;
			read += length;

			return _interactive;
		}

		private bool ReadN(int count, bool optional = false)
		{
			var index = 0;
			while (index < count)
			{
				var read = _inner.Read(_buffer16, _index16 + index, count - index);
				if (read == 0)
				{
					if (index == 0 && optional)
						return false;

					throw new IOException();
				}

				index += read;
			}

			_index16 += index;

			return true;
		}

		private void Read0() { _index16 = 0; }

		// ReSharper disable once UnusedMethodReturnValue.Local
		private ulong Read64()
		{
			ReadN(sizeof(ulong));
			return BitConverter.ToUInt64(_buffer16, _index16 - sizeof(ulong));
		}

		private uint? TryRead32()
		{
			if (!ReadN(sizeof(uint), true))
				return null;

			return BitConverter.ToUInt32(_buffer16, _index16 - sizeof(uint));
		}

		private uint Read32()
		{
			ReadN(sizeof(uint));
			return BitConverter.ToUInt32(_buffer16, _index16 - sizeof(uint));
		}

		private ushort Read16()
		{
			ReadN(sizeof(ushort));
			return BitConverter.ToUInt16(_buffer16, _index16 - sizeof(ushort));
		}

		private byte Read8()
		{
			ReadN(sizeof(byte));
			return _buffer16[_index16 - 1];
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
			if (!_leaveOpen)
				_inner.Dispose();
		}

		public override bool CanRead => _inner.CanRead;
		public override bool CanSeek => false;
		public override bool CanWrite => false;

		public override long Length
		{
			get
			{
				EnsureFrame();
				return _frameInfo?.ContentLength ?? -1;
			}
		}

		public override long Position
		{
			get => _position;
			set => throw InvalidOperation("SetPosition");
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

		private static InvalidDataException InvalidHeaderChecksum() =>
			new InvalidDataException("Invalid LZ4 frame header checksum");

		private static InvalidDataException MagicNumberExpected() =>
			new InvalidDataException("LZ4 frame magic number expected");
		
		private static InvalidDataException UnknownFrameVersion(int version) =>
			new InvalidDataException($"LZ4 frame version {version} is not supported");

		private InvalidOperationException InvalidOperation(string operation) =>
			new InvalidOperationException(
				$"Operation {operation} is not allowed for {GetType().Name}");
	}
}
