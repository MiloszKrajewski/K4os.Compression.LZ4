using System;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using K4os.Compression.LZ4.Encoders;
using K4os.Compression.LZ4.Internal;
using K4os.Compression.LZ4.Streams.Internal;

namespace K4os.Compression.LZ4.Streams.NewStreams
{
	/// <summary>
	/// LZ4 Decompression stream handling.
	/// </summary>
	public abstract partial class FrameDecoder<TStream> where TStream: IStreamReader
	{
		private ReaderTools<TStream> _reader;
		private readonly bool _interactive;

		private ILZ4Descriptor _descriptor;
		private ILZ4Decoder _decoder;
		
		private byte[] _buffer;
		private int _decoded;

		private long _bytesRead;

		private ref ReaderTools<TStream> Reader => ref _reader;

		/// <summary>Creates new instance <see cref="LZ4DecoderStream"/>.</summary>
		/// <param name="stream">Inner stream.</param>
		/// <param name="interactive">If <c>true</c> reading from stream will be "interactive" allowing
		/// to read bytes as soon as possible, even if more data is expected.</param>
		protected FrameDecoder(TStream stream, bool interactive = false)
		{
			_reader = new ReaderTools<TStream>(stream);
			_bytesRead = 0;
			_interactive = interactive;
		}
		
		private static int MaxBlockSize(int blockSizeCode) =>
			blockSizeCode switch {
				7 => Mem.M4, 6 => Mem.M1, 5 => Mem.K256, 4 => Mem.K64, _ => Mem.K64
			};

		protected abstract ILZ4Decoder CreateDecoder(ILZ4Descriptor descriptor);

		private void CloseFrame()
		{
			if (_decoder == null)
				return;

			try
			{
				_descriptor = null;
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
			_bytesRead += length;
			_decoded -= length;
			offset += length;
			count -= length;
			read += length;

			return _interactive;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		protected long GetBytesRead() => _bytesRead;
	
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		protected long GetFrameLength() =>
			GetFrameLength(EmptyToken.Value);

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		protected Task<long> GetFrameLengthAsync(CancellationToken token = default) =>
			GetFrameLength(token);
		
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		protected int ReadOneByte() =>
			ReadOneByte(EmptyToken.Value);

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		protected Task<int> ReadOneByteAsync(CancellationToken token = default) =>
			ReadOneByte(token);

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		protected Task<int> ReadManyBytesAsync(
			Memory<byte> buffer, CancellationToken token = default) =>
			ReadManyBytes(token, buffer);
		
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		protected int ReadManyBytes(Span<byte> buffer) =>
			ReadManyBytes(EmptyToken.Value, buffer);
		
		private static NotImplementedException NotImplemented(string feature) =>
			new($"Feature '{feature}' is not implemented");

		private static InvalidDataException InvalidHeaderChecksum() =>
			new("Invalid LZ4 frame header checksum");

		private static InvalidDataException MagicNumberExpected() =>
			new("LZ4 frame magic number expected");

		private static InvalidDataException UnknownFrameVersion(int version) =>
			new($"LZ4 frame version {version} is not supported");
	}
}
