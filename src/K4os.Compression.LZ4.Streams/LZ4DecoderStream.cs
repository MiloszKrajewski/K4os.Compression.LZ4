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
	/// LZ4 Decompression stream handling.
	/// </summary>
	public class LZ4DecoderStream: Stream, IDisposable
	{
		private readonly bool _interactive = true;
		private readonly bool _leaveOpen;

		private readonly Stream _inner;
		
		// ReSharper disable once InconsistentNaming
		private const int _length16 = 16; // we intend to use only 16 bytes
		private readonly byte[] _buffer16 = new byte[_length16 + 8];  
		private int _index16;

		private readonly Func<ILZ4Descriptor, ILZ4Decoder> _decoderFactory;

		private ILZ4Descriptor _frameInfo;
		private ILZ4Decoder _decoder;
		private int _decoded;
		private byte[] _buffer;

		private long _position;

		/// <summary>Creates new instance <see cref="LZ4DecoderStream"/>.</summary>
		/// <param name="inner">Inner stream.</param>
		/// <param name="decoderFactory">A function which will create appropriate decoder depending
		/// on frame descriptor.</param>
		/// <param name="leaveOpen">If <c>true</c> inner stream will not be closed after disposing.</param>
		public LZ4DecoderStream(
			Stream inner,
			Func<ILZ4Descriptor, ILZ4Decoder> decoderFactory,
			bool leaveOpen = false)
		{
			_inner = inner;
			_decoderFactory = decoderFactory;
			_leaveOpen = leaveOpen;
			_position = 0;
		}

		/// <inheritdoc />
		public override void Flush() =>
			_inner.Flush();

		/// <inheritdoc />
		public override Task FlushAsync(CancellationToken cancellationToken) =>
			_inner.FlushAsync(cancellationToken);

		/// <inheritdoc />
		public override int Read(byte[] buffer, int offset, int count)
		{
			if (!EnsureFrame())
				return 0;

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

		/// <inheritdoc />
		public override int ReadByte() =>
			Read(_buffer16, _length16, 1) > 0 ? _buffer16[_length16] : -1;

		private bool EnsureFrame() => _decoder != null || ReadFrame();

		[SuppressMessage("ReSharper", "InconsistentNaming")]
		private bool ReadFrame()
		{
			FlushPeek();

			var magic = TryPeek32();
			
			if (!magic.HasValue)
				return false;

			if (magic != 0x184D2204)
				throw MagicNumberExpected();

			FlushPeek();

			var FLG_BD = Peek16();

			var FLG = FLG_BD & 0xFF;
			var BD = (FLG_BD >> 8) & 0xFF;

			var version = (FLG >> 6) & 0x11;

			if (version != 1)
				throw UnknownFrameVersion(version);

			var blockChaining = ((FLG >> 5) & 0x01) == 0;
			var blockChecksum = ((FLG >> 4) & 0x01) != 0;
			var hasContentSize = ((FLG >> 3) & 0x01) != 0;
			var contentChecksum = ((FLG >> 2) & 0x01) != 0;
			var hasDictionary = (FLG & 0x01) != 0;
			var blockSizeCode = (BD >> 4) & 0x07;

			var contentLength = hasContentSize ? (long?) Peek64() : null;
			var dictionaryId = hasDictionary ? (uint?) Peek32() : null;

			var actualHC = (byte) (XXH32.DigestOf(_buffer16, 0, _index16) >> 8);
			var expectedHC = Peek8();

			if (actualHC != expectedHC)
				throw InvalidHeaderChecksum();

			var blockSize = MaxBlockSize(blockSizeCode);

			if (hasDictionary)
				throw NotImplemented(
					"Predefined dictionaries feature is not implemented"); // Write32(dictionaryId);

			// ReSharper disable once ExpressionIsAlwaysNull
			_frameInfo = new LZ4Descriptor(
				contentLength, contentChecksum, blockChaining, blockChecksum, dictionaryId,
				blockSize);
			_decoder = _decoderFactory(_frameInfo);
			_buffer = new byte[blockSize];

			return true;
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
			FlushPeek();

			var blockLength = (int) Peek32();
			if (blockLength == 0)
			{
				if (_frameInfo.ContentChecksum)
					Peek32();
				CloseFrame();
				return 0;
			}

			var uncompressed = (blockLength & 0x80000000) != 0;
			blockLength &= 0x7FFFFFFF;

			PeekN(_buffer, 0, blockLength);

			if (_frameInfo.BlockChecksum)
				Peek32();

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

		private int PeekN(byte[] buffer, int offset, int count, bool optional = false)
		{
			var index = 0;
			while (count > 0)
			{
				var read = _inner.Read(buffer, index + offset, count);
				if (read == 0)
				{
					if (index == 0 && optional)
						return 0;

					throw EndOfStream();
				}

				index += read;
				count -= read;
			}

			return index;
		}

		private bool PeekN(int count, bool optional = false)
		{
			if (count == 0) return true;

			var read = PeekN(_buffer16, _index16, count, optional);
			_index16 += read;
			return read > 0;
		}

		private void FlushPeek() { _index16 = 0; }

		// ReSharper disable once UnusedMethodReturnValue.Local
		private ulong Peek64()
		{
			PeekN(sizeof(ulong));
			return BitConverter.ToUInt64(_buffer16, _index16 - sizeof(ulong));
		}

		private uint? TryPeek32()
		{
			if (!PeekN(sizeof(uint), true))
				return null;

			return BitConverter.ToUInt32(_buffer16, _index16 - sizeof(uint));
		}

		private uint Peek32()
		{
			PeekN(sizeof(uint));
			return BitConverter.ToUInt32(_buffer16, _index16 - sizeof(uint));
		}

		private ushort Peek16()
		{
			PeekN(sizeof(ushort));
			return BitConverter.ToUInt16(_buffer16, _index16 - sizeof(ushort));
		}

		private byte Peek8()
		{
			PeekN(sizeof(byte));
			return _buffer16[_index16 - 1];
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

		private NotImplementedException NotImplemented(string operation) =>
			new NotImplementedException(
				$"Feature {operation} has not been implemented in {GetType().Name}");

		private static InvalidDataException InvalidHeaderChecksum() =>
			new InvalidDataException("Invalid LZ4 frame header checksum");

		private static InvalidDataException MagicNumberExpected() =>
			new InvalidDataException("LZ4 frame magic number expected");

		private static InvalidDataException UnknownFrameVersion(int version) =>
			new InvalidDataException($"LZ4 frame version {version} is not supported");

		private InvalidOperationException InvalidOperation(string operation) =>
			new InvalidOperationException(
				$"Operation {operation} is not allowed for {GetType().Name}");

		private static EndOfStreamException EndOfStream() =>
			new EndOfStreamException("Unexpected end of stream. Data might be corrupted.");
	}
}
