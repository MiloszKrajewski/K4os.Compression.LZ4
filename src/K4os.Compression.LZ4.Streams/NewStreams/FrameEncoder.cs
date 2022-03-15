using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using K4os.Compression.LZ4.Encoders;
using K4os.Compression.LZ4.Internal;
using K4os.Compression.LZ4.Streams.Internal;

namespace K4os.Compression.LZ4.Streams.NewStreams
{
	/// <summary>
	/// LZ4 stream encoder. 
	/// </summary>
	public partial class FrameEncoder<TStream>:
		IAsyncDisposable,
		IDisposable
		where TStream: IStreamWriter
	{
		private WriterTools<TStream> _writer;
		private readonly Func<ILZ4Descriptor, ILZ4Encoder> _encoderFactory;

		private ILZ4Descriptor _descriptor;
		private ILZ4Encoder _encoder;

		private byte[] _buffer;

		private long _bytesWritten;

		private ref WriterTools<TStream> Writer => ref _writer;

		/// <summary>Creates new instance of <see cref="LZ4EncoderStream"/>.</summary>
		/// <param name="inner">Inner stream.</param>
		/// <param name="encoderFactory">LZ4 Encoder factory.</param>
		/// <param name="descriptor">LZ4 Descriptor.</param>
		protected FrameEncoder(
			TStream inner,
			Func<ILZ4Descriptor, ILZ4Encoder> encoderFactory,
			ILZ4Descriptor descriptor)
		{
			_writer = new WriterTools<TStream>(inner);
			_encoderFactory = encoderFactory;
			_descriptor = descriptor;
			_bytesWritten = 0;
		}

		protected ILZ4Encoder CreateEncoder(ILZ4Descriptor descriptor) =>
			_encoderFactory(descriptor);

		[SuppressMessage("ReSharper", "InconsistentNaming")]
		private bool TryStashFrame()
		{
			if (_encoder != null)
				return false;

			Writer.Poke4(0x184D2204);

			var headerOffset = Writer.Length;

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

			Writer.Poke2((ushort)((FLG & 0xFF) | (BD & 0xFF) << 8));

			if (hasContentSize)
				throw NotImplemented(
					"ContentSize feature is not implemented"); // Stash8(contentSize);

			if (hasDictionary)
				throw NotImplemented(
					"Predefined dictionaries feature is not implemented"); // Stash4(dictionaryId);

			var HC = (byte)(Writer.Digest(headerOffset) >> 8);

			Writer.Poke1(HC);

			_encoder = CreateEncoder();
			_buffer = AllocateBuffer(LZ4Codec.MaximumOutputSize(blockSize));

			return true;
		}

		protected virtual byte[] AllocateBuffer(int size) => 
			new byte[size];

		protected virtual void ReleaseBuffer(byte[] buffer) { }

		private ILZ4Encoder CreateEncoder()
		{
			var encoder = CreateEncoder(_descriptor);
			if (encoder.BlockSize > _descriptor.BlockSize)
				throw InvalidValue("BlockSize is greater than declared");

			return encoder;
		}

		private BlockInfo TopupAndEncode(
			ReadOnlySpan<byte> buffer, ref int offset, ref int count)
		{
			var action = _encoder.TopupAndEncode(
				buffer.Slice(offset, count),
				_buffer.AsSpan(),
				false, true,
				out var loaded,
				out var encoded);

			_bytesWritten += loaded;
			offset += loaded;
			count -= loaded;

			return new BlockInfo(_buffer, action, encoded);
		}

		private BlockInfo FlushAndEncode()
		{
			var action = _encoder.FlushAndEncode(
				_buffer.AsSpan(), true, out var encoded);

			try
			{
				_encoder.Dispose();

				return new BlockInfo(_buffer, action, encoded);
			}
			finally
			{
				_encoder = null;
				_buffer = null;
			}
		}

		private static uint BlockLengthCode(in BlockInfo block) =>
			(uint)block.Length | (block.Compressed ? 0 : 0x80000000);

		// ReSharper disable once UnusedParameter.Local
		// NOTE: block will carry checksum one day
		private uint? BlockChecksum(BlockInfo block)
		{
			if (_descriptor.BlockChecksum)
				throw NotImplemented("BlockChecksum");

			return null;
		}

		private uint? ContentChecksum()
		{
			if (_descriptor.ContentChecksum)
				throw NotImplemented("ContentChecksum");

			return null;
		}

		private int MaxBlockSizeCode(int blockSize) =>
			blockSize <= Mem.K64 ? 4 :
			blockSize <= Mem.K256 ? 5 :
			blockSize <= Mem.M1 ? 6 :
			blockSize <= Mem.M4 ? 7 :
			throw InvalidBlockSize(blockSize);

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		protected long GetBytesWritten() => _bytesWritten;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		protected void WriteOneByte(byte value) =>
			WriteOneByte(EmptyToken.Value, value);

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		protected Task WriteOneByteAsync(byte value, CancellationToken token = default) =>
			WriteOneByte(token, value);

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		protected void WriteManyBytes(ReadOnlySpan<byte> buffer) =>
			WriteManyBytes(EmptyToken.Value, buffer);

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		protected Task WriteManyBytesAsync(
			ReadOnlyMemory<byte> buffer, CancellationToken token = default) =>
			WriteManyBytes(token, buffer);

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		protected void CloseFrame() => CloseFrame(EmptyToken.Value);

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		protected Task CloseFrameAsync(CancellationToken token = default) =>
			CloseFrame(token);

		private NotImplementedException NotImplemented(string operation) =>
			new($"Feature {operation} has not been implemented in {GetType().Name}");

		private static ArgumentException InvalidValue(string description) =>
			new(description);

		private protected ArgumentException InvalidBlockSize(int blockSize) =>
			InvalidValue($"Invalid block size ${blockSize} for {GetType().Name}");

		protected virtual void Dispose(bool disposing)
		{
			if (disposing)
				CloseFrame();
		}

		public void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		public async ValueTask DisposeAsync()
		{
			await CloseFrameAsync();
			GC.SuppressFinalize(this);
		}
	}
}
