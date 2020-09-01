//------------------------------------------------------------------------------
//
// This file has been generated. All changes will be lost.
//
//------------------------------------------------------------------------------
#define BLOCKING

using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using K4os.Hash.xxHash;

namespace K4os.Compression.LZ4.Streams
{
	public partial class LZ4DecoderStream
	{
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private void InnerFlush() =>
			_inner.Flush();

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private int InnerRead(
			byte[] buffer, int offset, int length) =>
			_inner.Read(buffer, offset, length);
		
		private /*async*/ int PeekN(
			byte[] buffer, int offset, int count, bool optional = false)
		{
			var index = 0;
			while (count > 0)
			{
				var read = /*await*/ InnerRead(buffer, index + offset, count);

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
		
		private /*async*/ bool PeekN(
			int count, bool optional = false)
		{
			if (count == 0) return true;

			var read = /*await*/ PeekN(_buffer16, _index16, count, optional);
			_index16 += read;
			return read > 0;
		}

		private /*async*/ ulong Peek8()
		{
			/*await*/ PeekN(sizeof(ulong));
			return BitConverter.ToUInt64(_buffer16, _index16 - sizeof(ulong));
		}

		private /*async*/ uint? TryPeek4()
		{
			if (!/*await*/ PeekN(sizeof(uint), true))
				return null;

			return BitConverter.ToUInt32(_buffer16, _index16 - sizeof(uint));
		}

		private /*async*/ uint Peek4()
		{
			/*await*/ PeekN(sizeof(uint));
			return BitConverter.ToUInt32(_buffer16, _index16 - sizeof(uint));
		}

		private /*async*/ ushort Peek2()
		{
			/*await*/ PeekN(sizeof(ushort));
			return BitConverter.ToUInt16(_buffer16, _index16 - sizeof(ushort));
		}

		private /*async*/ byte Peek1()
		{
			/*await*/ PeekN(sizeof(byte));
			return _buffer16[_index16 - 1];
		}
		
		private /*async*/ bool EnsureFrame() => 
			_decoder != null || /*await*/ ReadFrame();

		[SuppressMessage("ReSharper", "InconsistentNaming")]
		private /*async*/ bool ReadFrame()
		{
			FlushPeek();

			var magic = /*await*/ TryPeek4();
			
			if (!magic.HasValue)
				return false;

			if (magic != 0x184D2204)
				throw MagicNumberExpected();

			FlushPeek();

			var FLG_BD = /*await*/ Peek2();

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

			var contentLength = hasContentSize ? (long?) /*await*/ Peek8() : null;
			var dictionaryId = hasDictionary ? (uint?) /*await*/ Peek4() : null;

			var actualHC = (byte) (XXH32.DigestOf(_buffer16, 0, _index16) >> 8);
			
			var expectedHC = /*await*/ Peek1();

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
		
		private /*async*/ int ReadBlock()
		{
			FlushPeek();

			var blockLength = (int) /*await*/ Peek4();
			if (blockLength == 0)
			{
				if (_frameInfo.ContentChecksum)
					/*await*/ Peek4();
				CloseFrame();
				return 0;
			}

			var uncompressed = (blockLength & 0x80000000) != 0;
			blockLength &= 0x7FFFFFFF;

			/*await*/ PeekN(_buffer, 0, blockLength);

			if (_frameInfo.BlockChecksum)
				/*await*/ Peek4();

			return InjectOrDecode(blockLength, uncompressed);
		}
	}
}
