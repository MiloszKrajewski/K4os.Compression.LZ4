using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using K4os.Compression.LZ4.Encoders;
using K4os.Compression.LZ4.Internal;
using K4os.Compression.LZ4.Streams.Internal;

namespace K4os.Compression.LZ4.Streams.OldStreams
{
	/// <summary>
	/// LZ4 compression stream. 
	/// </summary>
	public partial class LZ4EncoderStream: LZ4StreamBase
	{
		private ILZ4Encoder _encoder;
		private readonly Func<ILZ4Descriptor, ILZ4Encoder> _encoderFactory;

		private readonly ILZ4Descriptor _descriptor;

		private byte[] _buffer;
		private long _position;

		/// <summary>Creates new instance of <see cref="LZ4EncoderStream"/>.</summary>
		/// <param name="inner">Inner stream.</param>
		/// <param name="descriptor">LZ4 Descriptor.</param>
		/// <param name="encoderFactory">Function which will take descriptor and return
		/// appropriate encoder.</param>
		/// <param name="leaveOpen">Indicates if <paramref name="inner"/> stream should be left
		/// open after disposing.</param>
		public LZ4EncoderStream(
			Stream inner,
			ILZ4Descriptor descriptor,
			Func<ILZ4Descriptor, ILZ4Encoder> encoderFactory,
			bool leaveOpen = false):
			base(inner, leaveOpen)
		{
			_descriptor = descriptor;
			_encoderFactory = encoderFactory;
		}

		[SuppressMessage("ReSharper", "InconsistentNaming")]
		private bool TryStashFrame()
		{
			if (_encoder != null)
				return false;

			Stash.Stash4(0x184D2204);

			var headerOffset = Stash.Length;

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

			Stash.Stash2((ushort) ((FLG & 0xFF) | (BD & 0xFF) << 8));

			if (hasContentSize)
				throw NotImplemented(
					"ContentSize feature is not implemented"); // Stash8(contentSize);

			if (hasDictionary)
				throw NotImplemented(
					"Predefined dictionaries feature is not implemented"); // Stash4(dictionaryId);

			var HC = (byte) (DigestOfStash(headerOffset) >> 8);

			Stash.Stash1(HC);

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

			_position += loaded;
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
			(uint) block.Length | (block.Compressed ? 0 : 0x80000000);

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

		private protected ArgumentException InvalidBlockSize(int blockSize) =>
			new ArgumentException($"Invalid block size ${blockSize} for {GetType().Name}");
	}
}
