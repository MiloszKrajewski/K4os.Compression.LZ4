//------------------------------------------------------------------------------
//
// This file has been generated. All changes will be lost.
//
//------------------------------------------------------------------------------
#define BLOCKING

using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using K4os.Compression.LZ4.Streams.Internal;
#if BLOCKING
using WritableBuffer = System.Span<byte>;
using Token = K4os.Compression.LZ4.Streams.Internal.EmptyToken;
#else
using System.Threading.Tasks;
using WritableBuffer = System.Memory<byte>;
using Token = System.Threading.CancellationToken;
#endif

namespace K4os.Compression.LZ4.Streams.NewStreams
{
	public partial class FrameDecoder<TStream>
	{
		#region Inner stream
		
		private /*async*/ ulong Peek8(Token token)
		{
			var loaded = /*await*/ Reader.Read(token, sizeof(ulong));
			return Reader.Last8(loaded);
		}

		private /*async*/ uint? TryPeek4(Token token)
		{
			var loaded = /*await*/ Reader.Read(token, sizeof(uint), true);
			return loaded <= 0 ? default(uint?) : Reader.Last4(loaded);
		}

		private /*async*/ uint Peek4(Token token)
		{
			var loaded = /*await*/ Reader.Read(token, sizeof(uint));
			return Reader.Last4(loaded);
		}

		private /*async*/ ushort Peek2(Token token)
		{
			var loaded = /*await*/ Reader.Read(token, sizeof(ushort));
			return Reader.Last2(loaded);
		}

		private /*async*/ byte Peek1(Token token)
		{
			var loaded = /*await*/ Reader.Read(token, sizeof(byte));
			return Reader.Last1(loaded);
		}

		private void ReadN(Token token, byte[] buffer, int offset, int length) => 
			Reader.Read(token, buffer, offset, length);
		
		#endregion
		
		#region The juice!

		private /*async*/ bool EnsureHeader(Token token) =>
			_decoder != null || /*await*/ ReadHeader(token);

		[SuppressMessage("ReSharper", "InconsistentNaming")]
		private /*async*/ bool ReadHeader(Token token)
		{
			Reader.Clear();

			var magic = /*await*/ TryPeek4(token);

			if (!magic.HasValue)
				return false;

			if (magic != 0x184D2204)
				throw MagicNumberExpected();

			var headerOffset = Reader.Length;

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

			var actualHC = (byte) (Reader.Digest(headerOffset) >> 8);

			var expectedHC = /*await*/ Peek1(token);

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
			_buffer = new byte[blockSize];

			return true;
		}

		private /*async*/ int ReadBlock(Token token)
		{
			Reader.Clear();

			var blockLength = (int) /*await*/ Peek4(token);
			if (blockLength == 0)
			{
				if (_descriptor.ContentChecksum)
					_ = /*await*/ Peek4(token);
				CloseFrame();
				return 0;
			}

			var uncompressed = (blockLength & 0x80000000) != 0;
			blockLength &= 0x7FFFFFFF;
			
			/*await*/ ReadN(token, _buffer, 0, blockLength);

			if (_descriptor.BlockChecksum)
				_ = /*await*/ Peek4(token);

			return InjectOrDecode(blockLength, uncompressed);
		}

		#endregion
		
		#region Interface
		
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		// ReSharper disable once UnusedParameter.Local
		private WritableBuffer OneByteBuffer(in Token _) =>
			#if BLOCKING
			Reader.OneByteSpan();
			#else
			Reader.OneByteMemory();
			#endif
		
		private /*async*/ long GetFrameLength(Token token)
		{
			/*await*/ EnsureHeader(token);
			return _descriptor?.ContentLength ?? -1;
		}
		
		private /*async*/ int ReadOneByte(Token token) =>
			/*await*/ ReadManyBytes(token, OneByteBuffer(token)) > 0 ? Reader.OneByteValue() : -1;
		
		private /*async*/ int ReadManyBytes(Token token, WritableBuffer buffer)
		{
			var hasFrame = /*await*/ EnsureHeader(token);
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
		
		#endregion
	}
}
