//------------------------------------------------------------------------------
//
// This file has been generated. All changes will be lost.
//
//------------------------------------------------------------------------------
#define BLOCKING

using System;
using System.Diagnostics.CodeAnalysis;

#if BLOCKING
using WritableBuffer = System.Span<byte>;
using Token = K4os.Compression.LZ4.Streams.EmptyToken;
#else
using System.Threading.Tasks;
using WritableBuffer = System.Memory<byte>;
using Token = System.Threading.CancellationToken;
#endif

namespace K4os.Compression.LZ4.Streams
{
	public partial class LZ4DecoderStream
	{
		private /*async*/ int PeekN(
			Token token, 
			byte[] buffer, int offset, int count, 
			bool optional = false)
		{
			var index = 0;
			while (count > 0)
			{
				var read = /*await*/ InnerRead(token, buffer, index + offset, count);

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

		private /*async*/ bool PeekN(Token token, int count, bool optional = false)
		{
			if (count == 0) return true;

			var read = /*await*/ PeekN(token, _buffer16, _index16, count, optional);
			_index16 += read;
			return read > 0;
		}

		private /*async*/ ulong Peek8(Token token)
		{
			/*await*/ PeekN(token, sizeof(ulong));
			return BitConverter.ToUInt64(_buffer16, _index16 - sizeof(ulong));
		}

		private /*async*/ uint? TryPeek4(Token token)
		{
			if (!/*await*/ PeekN(token, sizeof(uint), true))
				return null;

			return BitConverter.ToUInt32(_buffer16, _index16 - sizeof(uint));
		}

		private /*async*/ uint Peek4(Token token)
		{
			/*await*/ PeekN(token, sizeof(uint));
			return BitConverter.ToUInt32(_buffer16, _index16 - sizeof(uint));
		}

		private /*async*/ ushort Peek2(Token token)
		{
			/*await*/ PeekN(token, sizeof(ushort));
			return BitConverter.ToUInt16(_buffer16, _index16 - sizeof(ushort));
		}

		private /*async*/ byte Peek1(Token token)
		{
			/*await*/ PeekN(token, sizeof(byte));
			return _buffer16[_index16 - 1];
		}

		private /*async*/ bool EnsureFrame(Token token) =>
			_decoder != null || /*await*/ ReadFrame(token);

		[SuppressMessage("ReSharper", "InconsistentNaming")]
		private /*async*/ bool ReadFrame(Token token)
		{
			FlushPeek();

			var magic = /*await*/ TryPeek4(token);

			if (!magic.HasValue)
				return false;

			if (magic != 0x184D2204)
				throw MagicNumberExpected();

			FlushPeek();

			var FLG_BD = /*await*/ Peek2(token);

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

			var contentLength = hasContentSize ? (long?) /*await*/ Peek8(token) : null;
			var dictionaryId = hasDictionary ? (uint?) /*await*/ Peek4(token) : null;

			var actualHC = (byte) (DigestOfStash() >> 8);

			var expectedHC = /*await*/ Peek1(token);

			if (actualHC != expectedHC)
				throw InvalidHeaderChecksum();

			var blockSize = MaxBlockSize(blockSizeCode);

			if (hasDictionary)
				throw NotImplemented(
					"Predefined dictionaries feature is not implemented"); // Peek4(dictionaryId);

			// ReSharper disable once ExpressionIsAlwaysNull
			_frameInfo = new LZ4Descriptor(
				contentLength, contentChecksum, blockChaining, blockChecksum, dictionaryId,
				blockSize);
			_decoder = _decoderFactory(_frameInfo);
			_buffer = new byte[blockSize];

			return true;
		}
		
		private /*async*/ long GetLength(Token token)
		{
			/*await*/ EnsureFrame(token);
			return _frameInfo?.ContentLength ?? -1;
		}

		private /*async*/ int ReadBlock(Token token)
		{
			FlushPeek();

			var blockLength = (int) /*await*/ Peek4(token);
			if (blockLength == 0)
			{
				if (_frameInfo.ContentChecksum)
					_ = /*await*/ Peek4(token);
				CloseFrame();
				return 0;
			}

			var uncompressed = (blockLength & 0x80000000) != 0;
			blockLength &= 0x7FFFFFFF;

			/*await*/ PeekN(token, _buffer, 0, blockLength);

			if (_frameInfo.BlockChecksum)
				_ = /*await*/ Peek4(token);

			return InjectOrDecode(blockLength, uncompressed);
		}

		private /*async*/ int ReadImpl(Token token, WritableBuffer buffer)
		{
			if (!/*await*/ EnsureFrame(token))
				return 0;

			var offset = 0;
			var count = buffer.Length;

			var read = 0;
			while (count > 0)
			{
				if (_decoded <= 0 && (_decoded = /*await*/ ReadBlock(token)) == 0)
					break;

				if (Drain(buffer.ToSpan(), ref offset, ref count, ref read))
					break;
			}

			return read;
		}
		
		#if BLOCKING || NETSTANDARD2_1

		private /*async*/ void DisposeImpl(Token token)
		{
			CloseFrame();
			if (!_leaveOpen) /*await*/ InnerDispose(token);
		}

		#endif
	}
}
