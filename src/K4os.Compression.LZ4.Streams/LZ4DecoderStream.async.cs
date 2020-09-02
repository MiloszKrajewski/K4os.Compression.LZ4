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
		private async Task<int> PeekN(
			Token token,
			byte[] buffer, int offset, int count,
			bool optional = false)
		{
			var index = 0;
			while (count > 0)
			{
				var read = await InnerRead(token, buffer, index + offset, count).Weave();

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

		private async Task<bool> PeekN(Token token, int count, bool optional = false)
		{
			if (count == 0) return true;

			var read = await PeekN(token, _buffer16, _index16, count, optional).Weave();
			_index16 += read;
			return read > 0;
		}

		private async Task<ulong> Peek8(Token token)
		{
			_ = await PeekN(token, sizeof(ulong)).Weave();
			return BitConverter.ToUInt64(_buffer16, _index16 - sizeof(ulong));
		}

		private async Task<uint?> TryPeek4(Token token)
		{
			var loaded = await PeekN(token, sizeof(uint), true).Weave();
			if (!loaded) return null;

			return BitConverter.ToUInt32(_buffer16, _index16 - sizeof(uint));
		}

		private async Task<uint> Peek4(Token token)
		{
			_ = await PeekN(token, sizeof(uint)).Weave();
			return BitConverter.ToUInt32(_buffer16, _index16 - sizeof(uint));
		}

		private async Task<ushort> Peek2(Token token)
		{
			_ = await PeekN(token, sizeof(ushort)).Weave();
			return BitConverter.ToUInt16(_buffer16, _index16 - sizeof(ushort));
		}

		private async Task<byte> Peek1(Token token)
		{
			_ = await PeekN(token, sizeof(byte)).Weave();
			return _buffer16[_index16 - 1];
		}

		private async Task<bool> EnsureFrame(Token token) =>
			_decoder != null || await ReadFrame(token).Weave();

		[SuppressMessage("ReSharper", "InconsistentNaming")]
		private async Task<bool> ReadFrame(Token token)
		{
			FlushPeek();

			var magic = await TryPeek4(token).Weave();

			if (!magic.HasValue)
				return false;

			if (magic != 0x184D2204)
				throw MagicNumberExpected();

			FlushPeek();

			var FLG_BD = await Peek2(token).Weave();

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

			var contentLength = hasContentSize ? (long?) await Peek8(token).Weave() : null;
			var dictionaryId = hasDictionary ? (uint?) await Peek4(token).Weave() : null;

			var actualHC = (byte) (DigestOfStash() >> 8);

			var expectedHC = await Peek1(token).Weave();

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

		private async Task<long> GetLength(Token token)
		{
			await EnsureFrame(token).Weave();
			return _frameInfo?.ContentLength ?? -1;
		}

		#endif

		private async Task<int> ReadBlock(Token token)
		{
			FlushPeek();

			var blockLength = (int) await Peek4(token).Weave();
			if (blockLength == 0)
			{
				if (_frameInfo.ContentChecksum)
					_ = await Peek4(token).Weave();
				CloseFrame();
				return 0;
			}

			var uncompressed = (blockLength & 0x80000000) != 0;
			blockLength &= 0x7FFFFFFF;

			await PeekN(token, _buffer, 0, blockLength).Weave();

			if (_frameInfo.BlockChecksum)
				_ = await Peek4(token).Weave();

			return InjectOrDecode(blockLength, uncompressed);
		}

		private async Task<int> ReadImpl(Token token, WritableBuffer buffer)
		{
			var hasFrame = await EnsureFrame(token).Weave();
			if (!hasFrame) return 0;

			var offset = 0;
			var count = buffer.Length;

			var read = 0;
			while (count > 0)
			{
				if (_decoded <= 0 && (_decoded = await ReadBlock(token).Weave()) == 0)
					break;

				if (Drain(buffer.ToSpan(), ref offset, ref count, ref read))
					break;
			}

			return read;
		}

		#if BLOCKING || NETSTANDARD2_1

		private async Task DisposeImpl(Token token)
		{
			CloseFrame();
			if (!_leaveOpen) await InnerDispose(token).Weave();
		}

		#endif
	}
}
