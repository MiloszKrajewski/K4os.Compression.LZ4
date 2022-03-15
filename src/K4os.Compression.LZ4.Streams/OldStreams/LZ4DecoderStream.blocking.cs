//------------------------------------------------------------------------------
//
// This file has been generated. All changes will be lost.
//
//------------------------------------------------------------------------------
#define BLOCKING

using System;
using System.Diagnostics.CodeAnalysis;
using K4os.Compression.LZ4.Streams.Internal;
#if BLOCKING
using WritableBuffer = System.Span<byte>;
using Token = K4os.Compression.LZ4.Streams.Internal.EmptyToken;
#else
using System.Threading.Tasks;
using WritableBuffer = System.Memory<byte>;
using Token = System.Threading.CancellationToken;
#endif

namespace K4os.Compression.LZ4.Streams
{
	public partial class LZ4DecoderStream
	{
		private /*async*/ ulong Peek8(Token token)
		{
			var loaded = /*await*/ Stash.Load(token, sizeof(ulong));
			return Stash.Last8(loaded);
		}

		private /*async*/ uint? TryPeek4(Token token)
		{
			var loaded = /*await*/ Stash.Load(token, sizeof(uint), true);
			return loaded <= 0 ? default(uint?) : Stash.Last4(loaded);
		}

		private /*async*/ uint Peek4(Token token)
		{
			var loaded = /*await*/ Stash.Load(token, sizeof(uint));
			return Stash.Last4(loaded);
		}

		private /*async*/ ushort Peek2(Token token)
		{
			var loaded = /*await*/ Stash.Load(token, sizeof(ushort));
			return Stash.Last2(loaded);
		}

		private /*async*/ byte Peek1(Token token)
		{
			var loaded = /*await*/ Stash.Load(token, sizeof(byte));
			return Stash.Last1(loaded);
		}

		private /*async*/ bool EnsureFrame(Token token) =>
			_decoder != null || /*await*/ ReadFrame(token);

		[SuppressMessage("ReSharper", "InconsistentNaming")]
		private /*async*/ bool ReadFrame(Token token)
		{
			Stash.Clear();

			var magic = /*await*/ TryPeek4(token);

			if (!magic.HasValue)
				return false;

			if (magic != 0x184D2204)
				throw MagicNumberExpected();

			var headerOffset = Stash.Length;

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

			var actualHC = (byte) (DigestOfStash(headerOffset) >> 8);

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

		#if BLOCKING
		private /*async*/ long GetLength(Token token)
		{
			/*await*/ EnsureFrame(token);
			return _frameInfo?.ContentLength ?? -1;
		}

		#endif

		private /*async*/ int ReadBlock(Token token)
		{
			Stash.Clear();

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

			/*await*/ InnerReadBlock(token, _buffer, 0, blockLength);

			if (_frameInfo.BlockChecksum)
				_ = /*await*/ Peek4(token);

			return InjectOrDecode(blockLength, uncompressed);
		}

		private /*async*/ int ReadImpl(Token token, WritableBuffer buffer)
		{
			var hasFrame = /*await*/ EnsureFrame(token);
			if (!hasFrame) return 0;

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
			/*await*/ InnerDispose(token, false);
		}

		#endif
	}
}
