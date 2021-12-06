﻿using System;
using System.IO;
using K4os.Compression.LZ4.Encoders;
using K4os.Compression.LZ4.Internal;

namespace K4os.Compression.LZ4.Streams.NewStreams
{
	/// <summary>
	/// LZ4 Decompression stream handling.
	/// </summary>
	public partial class StreamDecoder<TStream> 
		where TStream: IStreamReader
	{
		private readonly ReaderTools<TStream> _tools;
		private readonly bool _interactive;

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
		public StreamDecoder(
			TStream inner,
			Func<ILZ4Descriptor, ILZ4Decoder> decoderFactory,
			bool interactive = false)
		{
			_tools = new ReaderTools<TStream>(inner, 16);
			_decoderFactory = decoderFactory;
			_position = 0;
			_interactive = interactive;
		}
		
		private static int MaxBlockSize(int blockSizeCode) =>
			blockSizeCode switch {
				7 => Mem.M4, 6 => Mem.M1, 5 => Mem.K256, 4 => Mem.K64, _ => Mem.K64
			};

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
		
		private int InjectOrDecode(int blockLength, bool uncompressed) =>
			uncompressed
				? _decoder.Inject(_buffer, 0, blockLength)
				: _decoder.Decode(_buffer, 0, blockLength);

		private bool Drain(Span<byte> buffer, ref int offset, ref int count, ref int read)
		{
			if (_decoded <= 0)
				return true;

			var length = Math.Min(count, _decoded);
			_decoder.Drain(buffer.Slice(offset), -_decoded, length);
			_position += length;
			_decoded -= length;
			offset += length;
			count -= length;
			read += length;

			return _interactive;
		}

		private static InvalidDataException InvalidHeaderChecksum() =>
			new InvalidDataException("Invalid LZ4 frame header checksum");

		private static InvalidDataException MagicNumberExpected() =>
			new InvalidDataException("LZ4 frame magic number expected");

		private static InvalidDataException UnknownFrameVersion(int version) =>
			new InvalidDataException($"LZ4 frame version {version} is not supported");
	}
}
