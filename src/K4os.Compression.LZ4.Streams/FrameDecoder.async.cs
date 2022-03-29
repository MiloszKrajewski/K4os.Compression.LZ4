#if BLOCKING
using WritableBuffer = System.Span<byte>;
using Token = K4os.Compression.LZ4.Streams.Internal.EmptyToken;
#else
using System.Threading.Tasks;
using WritableBuffer = System.Memory<byte>;
using Token = System.Threading.CancellationToken;
#endif
using System;
using System.Diagnostics.CodeAnalysis;

using K4os.Compression.LZ4.Streams.Internal;

namespace K4os.Compression.LZ4.Streams;

public partial class FrameDecoder<TStream>
{
		private async Task<ulong> Peek8(Token token)
		{
			var loaded = await Reader.Read(token, sizeof(ulong)).Weave();
			return Reader.Last8(loaded);
		}

		private async Task<uint?> TryPeek4(Token token)
		{
			var loaded = await Reader.Read(token, sizeof(uint), true).Weave();
			return loaded <= 0 ? default(uint?) : Reader.Last4(loaded);
		}

		private async Task<uint> Peek4(Token token)
		{
			var loaded = await Reader.Read(token, sizeof(uint)).Weave();
			return Reader.Last4(loaded);
		}

		private async Task<ushort> Peek2(Token token)
		{
			var loaded = await Reader.Read(token, sizeof(ushort)).Weave();
			return Reader.Last2(loaded);
		}

		private async Task<byte> Peek1(Token token)
		{
			var loaded = await Reader.Read(token, sizeof(byte)).Weave();
			return Reader.Last1(loaded);
		}

		private async Task<bool> EnsureHeader(Token token) =>
			_decoder != null || await ReadHeader(token).Weave();
		
		[SuppressMessage("ReSharper", "InconsistentNaming")]
		private async Task<bool> ReadHeader(Token token)
		{
			Reader.Clear();

			var magic = await TryPeek4(token).Weave();

			if (!magic.HasValue)
				return false;

			if (magic != 0x184D2204)
				throw MagicNumberExpected();

			var headerOffset = Reader.Length;

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

			var contentLength = hasContentSize ? (long?)await Peek8(token).Weave() : null;
			var dictionaryId = hasDictionary ? (uint?)await Peek4(token).Weave() : null;

			var actualHC = (byte)(Reader.Digest(headerOffset) >> 8);

			var expectedHC = await Peek1(token).Weave();

			if (actualHC != expectedHC)
				throw InvalidHeaderChecksum();

			var blockSize = MaxBlockSize(blockSizeCode);

			if (hasDictionary)
				throw NotImplemented(
					"Predefined dictionaries feature is not implemented"); // Peek4(dictionaryId);

			// ReSharper disable once ExpressionIsAlwaysNull
			_descriptor = new LZ4Descriptor(
				contentLength, contentChecksum, blockChaining, blockChecksum, dictionaryId,
				blockSize);
			_decoder = CreateDecoder(_descriptor);
			_buffer = AllocBuffer(blockSize);

			return true;
		}

		private async Task<int> ReadBlock(Token token)
		{
			Reader.Clear();

			var blockLength = (int)await Peek4(token).Weave();
			if (blockLength == 0)
			{
				if (_descriptor.ContentChecksum)
					_ = await Peek4(token).Weave();
				CloseFrame();
				return 0;
			}

			var uncompressed = (blockLength & 0x80000000) != 0;
			blockLength &= 0x7FFFFFFF;

			await Reader.Read(token, _buffer, 0, blockLength).Weave();

			if (_descriptor.BlockChecksum)
				_ = await Peek4(token).Weave();

			return InjectOrDecode(blockLength, uncompressed);
		}

		private async Task<long?> GetFrameLength(Token token)
		{
			await EnsureHeader(token).Weave();
			return _descriptor?.ContentLength;
		}

		private async Task<int> ReadOneByte(Token token) =>
			await ReadManyBytes(token, Reader.OneByteBuffer(token)) > 0
				? Reader.OneByteValue()
				: -1;

		private async Task<int> ReadManyBytes(
			Token token, WritableBuffer buffer, bool interactive = false)
		{
			var hasFrame = await EnsureHeader(token).Weave();
			if (!hasFrame) return 0;

			var offset = 0;
			var count = buffer.Length;

			var read = 0;
			while (count > 0)
			{
				if (_decoded <= 0 && (_decoded = await ReadBlock(token).Weave()) == 0)
					break;

				var empty = Drain(buffer.ToSpan(), ref offset, ref count, ref read); 

				if (empty || interactive)
					break;
			}

			return read;
		}
}
