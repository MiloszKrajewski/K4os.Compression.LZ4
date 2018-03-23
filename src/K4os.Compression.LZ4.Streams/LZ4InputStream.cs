using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using K4os.Compression.LZ4.Encoders;

namespace K4os.Compression.LZ4.Streams
{
	public class LZ4InputStream: Stream
	{
		private readonly Stream _inner;
		private readonly byte[] _bytes = new byte[16];

		private int _decoded = 0;
		private bool _interactive = true;
		private readonly Func<ILZ4FrameInfo, ILZ4StreamDecoder> _decoderFactory;
		private ILZ4StreamDecoder _decoder = null;
		private ILZ4FrameInfo _frameInfo = null;

		public LZ4InputStream(Stream inner, Func<ILZ4FrameInfo, ILZ4StreamDecoder> decoderFactory)
		{
			_inner = inner;
			_decoderFactory = decoderFactory;
		}

		public override void Flush() =>
			_inner.Flush();

		public override Task FlushAsync(CancellationToken cancellationToken) =>
			_inner.FlushAsync(cancellationToken);

		public override int Read(byte[] buffer, int offset, int count)
		{
			var read = 0;
			while (count > 0)
			{
				if (_decoder == null)
					ReadFrame();

				if (_decoded <= 0)
					ReadBlock();

				if (ReadDecoded(buffer, ref offset, ref count, ref read))
					break;
			}

			return read;
		}

		[SuppressMessage("ReSharper", "InconsistentNaming")]
		private void ReadFrame()
		{
			var magic = TryRead32();
			if (magic != 0x184D2204)
				throw new InvalidDataException();

			var FLG_BD = Read16();

			var FLG = FLG_BD & 0xFF;

			var chaining = ((FLG >> 5) & 0x01) == 0;
			var checksum = ((FLG >> 4) & 0x01) != 0;

			var BD = (FLG_BD >> 8) & 0xFF;

			var contentSize = (BD & (1 << 3)) != 0 ? (long?) Read64() : null;
			var dictionaryId = (BD & (1 << 0)) != 0 ? (uint?) Read32() : null;

			var HC = ReadByte();

			var blockSize = MaxBlockSize((BD >> 3) & 0x07);

			_frameInfo = new LZ4FrameInfo(chaining, checksum, dictionaryId, blockSize);

			_decoder = _decoderFactory(_frameInfo);
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

		private int ReadBlock()
		{
			var blockLength = Read32();
			if ((blockLength & 0x80000000) != 0)
			{
				
			}
			#warning implement me
			return 0;
		}

		private bool ReadDecoded(byte[] buffer, ref int offset, ref int count, ref int read)
		{
			if (_decoded <= 0)
				return true;

			var length = Math.Max(count, _decoded);
			_decoder.Drain(buffer, offset, -_decoded, length);
			_decoded -= length;
			offset += length;
			count -= length;
			read += length;

			return _interactive;
		}

		private ulong Read64() => throw new NotImplementedException();
		private uint TryRead32() => throw new NotImplementedException();
		private uint Read32() => throw new NotImplementedException();
		private ushort Read16() => throw new NotImplementedException();

		public override Task<int> ReadAsync(
			byte[] buffer, int offset, int count, CancellationToken cancellationToken) =>
			Task.FromResult(Read(buffer, offset, count));

		public override int ReadByte() => Read(_bytes, 0, 1) > 0 ? _bytes[0] : -1;

		protected override void Dispose(bool disposing)
		{
			if (!disposing)
				return;

			_inner.Dispose();
		}

		public override bool CanRead => _inner.CanRead;
		public override bool CanSeek => false;
		public override bool CanWrite => false;
		public override long Length => -1;

		public override long Position
		{
			get => -1;
			set => throw new InvalidOperationException();
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

		private Exception InvalidOperation(string operation) =>
			new InvalidOperationException($"Operation {operation} is not allowed for {GetType().Name}");
	}
}
