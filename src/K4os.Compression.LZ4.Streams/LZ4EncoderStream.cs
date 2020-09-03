using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using K4os.Compression.LZ4.Encoders;
using K4os.Compression.LZ4.Internal;
using K4os.Hash.xxHash;

namespace K4os.Compression.LZ4.Streams
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
		
		// ReSharper disable once InconsistentNaming
		private const int _length16 = 16;
		private readonly byte[] _buffer16 = new byte[_length16 + 8];
		private int _index16;
		
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
				return _index16 > 0;
			
			Stash4(0x184D2204);

			var headerIndex = _index16;

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

			Stash2((ushort) ((FLG & 0xFF) | (BD & 0xFF) << 8));

			if (hasContentSize)
				throw NotImplemented(
					"ContentSize feature is not implemented"); // Stash8(contentSize);

			if (hasDictionary)
				throw NotImplemented(
					"Predefined dictionaries feature is not implemented"); // Stash4(dictionaryId);

			var HC = (byte) (DigestOfStash(headerIndex) >> 8);

			Stash1(HC);

			_encoder = CreateEncoder();
			_buffer = new byte[LZ4Codec.MaximumOutputSize(blockSize)];
			
			return _index16 > 0;
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
		
		private protected uint DigestOfStash(int offset = 0) => 
			XXH32.DigestOf(_buffer16, offset, _index16 - offset);

		private void StashBlockLength(BlockInfo block) =>
			Stash4((uint) block.Length | (block.Compressed ? 0 : 0x80000000));

		// ReSharper disable once UnusedParameter.Local
		private void StashBlockChecksum(BlockInfo block)
		{
			// NOTE: block will carry checksum one day

			if (_descriptor.BlockChecksum)
				throw NotImplemented("BlockChecksum");
		}

		private void StashStreamEnd()
		{
			Stash4(0);

			if (_descriptor.ContentChecksum)
				throw NotImplemented("ContentChecksum");
		}
		
		private int MaxBlockSizeCode(int blockSize) =>
			blockSize <= Mem.K64 ? 4 :
			blockSize <= Mem.K256 ? 5 :
			blockSize <= Mem.M1 ? 6 :
			blockSize <= Mem.M4 ? 7 :
			throw InvalidBlockSize(blockSize);

		private void Stash1(byte value)
		{
			_buffer16[_index16 + 0] = value;
			_index16++;
		}

		private void Stash2(ushort value)
		{
			_buffer16[_index16 + 0] = (byte) (value >> 0);
			_buffer16[_index16 + 1] = (byte) (value >> 8);
			_index16 += 2;
		}

		private void Stash4(uint value)
		{
			_buffer16[_index16 + 0] = (byte) (value >> 0);
			_buffer16[_index16 + 1] = (byte) (value >> 8);
			_buffer16[_index16 + 2] = (byte) (value >> 16);
			_buffer16[_index16 + 3] = (byte) (value >> 24);
			_index16 += 4;
		}

		/*
		private void Stash8(ulong value)
		{
		    _buffer16[_index16 + 0] = (byte) (value >> 0);
		    _buffer16[_index16 + 1] = (byte) (value >> 8);
		    _buffer16[_index16 + 2] = (byte) (value >> 16);
		    _buffer16[_index16 + 3] = (byte) (value >> 24);
		    _buffer16[_index16 + 4] = (byte) (value >> 32);
		    _buffer16[_index16 + 5] = (byte) (value >> 40);
		    _buffer16[_index16 + 6] = (byte) (value >> 48);
		    _buffer16[_index16 + 7] = (byte) (value >> 56);
		    _index16 += 8;
		}
		*/

		private int ClearStash()
		{
			var length = _index16;
			_index16 = 0;
			return length;
		}

		internal readonly struct BlockInfo
		{
			private readonly byte[] _buffer;
			private readonly int _length;

			public byte[] Buffer => _buffer;
			public int Offset => 0;
			public int Length => Math.Abs(_length);
			public bool Compressed => _length > 0;
			public bool Ready => _length != 0;

			public BlockInfo(byte[] buffer, EncoderAction action, int length)
			{
				_buffer = buffer;
				_length = action switch {
					EncoderAction.Encoded => length,
					EncoderAction.Copied => -length,
					_ => 0,
				};
			}
		}
		
		private protected ArgumentException InvalidBlockSize(int blockSize) =>
			new ArgumentException($"Invalid block size ${blockSize} for {GetType().Name}");
	}
}
