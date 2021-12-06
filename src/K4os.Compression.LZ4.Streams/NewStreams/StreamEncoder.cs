using System;
using System.Diagnostics.CodeAnalysis;
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
	public partial class StreamEncoder<TStream> 
		where TStream: IStreamWriter
	{
		private WriterTools<TStream> _tools;

		private ILZ4Encoder _encoder;
		private readonly Func<ILZ4Descriptor, ILZ4Encoder> _encoderFactory;

		private readonly ILZ4Descriptor _descriptor;

		private byte[] _buffer;
		private long _bytesWritten;

		private ref WriterTools<TStream> Tools => ref _tools;

		/// <summary>Creates new instance of <see cref="LZ4EncoderStream"/>.</summary>
		/// <param name="inner">Inner stream.</param>
		/// <param name="descriptor">LZ4 Descriptor.</param>
		/// <param name="encoderFactory">Function which will take descriptor and return
		/// appropriate encoder.</param>
		public StreamEncoder(
			TStream inner,
			ILZ4Descriptor descriptor,
			Func<ILZ4Descriptor, ILZ4Encoder> encoderFactory)
		{
			_tools = new WriterTools<TStream>(inner, 16);
			_descriptor = descriptor;
			_encoderFactory = encoderFactory;
			_bytesWritten = 0;
		}
		
		public long BytesWritten => _bytesWritten;

		public void Write(ReadOnlySpan<byte> buffer) => 
			WriteBytesImpl(EmptyToken.Value, buffer);
		
		public Task Write(ReadOnlyMemory<byte> buffer, CancellationToken token) => 
			WriteBytesImpl(token, buffer);

		[SuppressMessage("ReSharper", "InconsistentNaming")]
		private bool TryStashFrame()
		{
			if (_encoder != null)
				return false;

			Tools.Stash4(0x184D2204);

			var headerOffset = Tools.Length;

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

			Tools.Stash2((ushort)((FLG & 0xFF) | (BD & 0xFF) << 8));

			if (hasContentSize)
				throw NotImplemented(
					"ContentSize feature is not implemented"); // Stash8(contentSize);

			if (hasDictionary)
				throw NotImplemented(
					"Predefined dictionaries feature is not implemented"); // Stash4(dictionaryId);

			var HC = (byte)(Tools.Digest(headerOffset) >> 8);

			Tools.Stash1(HC);

			_encoder = CreateEncoder();
			_buffer = new byte[LZ4Codec.MaximumOutputSize(blockSize)];

			return true;
		}

		private ILZ4Encoder CreateEncoder()
		{
			var encoder = _encoderFactory(_descriptor);
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
		
		[SuppressMessage("ReSharper", "UnusedParameter.Local")]
		private void InnerWriteBlock(
			EmptyToken _, byte[] blockBuffer, int blockOffset, int blockLength) =>
			Stream.Write(blockBuffer, blockOffset, blockLength);

		private Task InnerWriteBlock(
			CancellationToken token, byte[] blockBuffer, int blockOffset, int blockLength) =>
			Stream.WriteAsync(blockBuffer, blockOffset, blockLength, token);

		private NotImplementedException NotImplemented(string operation) =>
			new($"Feature {operation} has not been implemented in {GetType().Name}");

		private static ArgumentException InvalidValue(string description) =>
			new(description);

		private protected ArgumentException InvalidBlockSize(int blockSize) =>
			InvalidValue($"Invalid block size ${blockSize} for {GetType().Name}");
	}
}
