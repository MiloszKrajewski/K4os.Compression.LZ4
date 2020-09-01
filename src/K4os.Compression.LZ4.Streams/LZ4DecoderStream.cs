using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using K4os.Compression.LZ4.Encoders;
using K4os.Compression.LZ4.Internal;
using K4os.Hash.xxHash;

namespace K4os.Compression.LZ4.Streams
{
	/// <summary>
	/// LZ4 Decompression stream handling.
	/// </summary>
	public partial class LZ4DecoderStream: Stream, IDisposable
	{
		// ReSharper disable once InconsistentNaming
		private const int _length16 = 16; // we intend to use only 16 bytes
		private readonly byte[] _buffer16 = new byte[_length16 + 8];
		private int _index16;

		private readonly bool _leaveOpen;
		private readonly bool _interactive;

		private readonly Stream _inner;
		
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
		/// <param name="interactive">If <c>true</c> reading from stream will be "interactive" allowing
		/// to read bytes as soon as possible, even if more data is expected.</param>
		public LZ4DecoderStream(
			Stream inner,
			Func<ILZ4Descriptor, ILZ4Decoder> decoderFactory,
			bool leaveOpen = false,
			bool interactive = false)
		{
			_inner = inner;
			_decoderFactory = decoderFactory;
			_leaveOpen = leaveOpen;
			_position = 0;
			_interactive = interactive;
		}
		
		private static int MaxBlockSize(int blockSizeCode) =>
			blockSizeCode switch {
				7 => Mem.M4, 6 => Mem.M1, 5 => Mem.K256, 4 => Mem.K64, _ => Mem.K64
			};

		private void FlushPeek() { _index16 = 0; }
		
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
		
		private unsafe int InjectOrDecode(int blockLength, bool uncompressed)
		{
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
