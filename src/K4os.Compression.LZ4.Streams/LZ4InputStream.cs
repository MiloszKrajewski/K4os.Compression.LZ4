using System;
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
		private readonly Func<LZ4FrameInfo, ILZ4StreamDecoder> _decoderFactory;
		private ILZ4StreamDecoder _decoder = null;

		public LZ4InputStream(Stream inner, Func<LZ4FrameInfo, ILZ4StreamDecoder> decoderFactory)
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
				if (_decoded <= 0)
					_decoded = ReadFrame();

				if (ReadDecoded(buffer, ref offset, ref count, ref read))
					break;
			}

			return read;
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

		private int ReadFrame()
		{
			var magic = TryRead32();
			var FLG_BD = Read16();
			var FLG = FLG_BD & 0xFF;
			var BD = (FLG_BD >> 8) & 0xFF;
			var contentSize = (BD & (1 << 3)) != 0 ? (long?) Read64() : null;
			var dictionaryId = (BD & (1 << 0)) != 0 ? (uint?) Read32() : null;
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
