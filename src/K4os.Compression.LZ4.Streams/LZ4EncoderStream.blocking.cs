//------------------------------------------------------------------------------
//
// This file has been generated. All changes will be lost.
//
//------------------------------------------------------------------------------
#define BLOCKING

using System;

#if BLOCKING
using ReadableBuffer = System.ReadOnlySpan<byte>;
using Token = K4os.Compression.LZ4.Streams.EmptyToken;
#else
using System.Threading.Tasks;
using ReadableBuffer = System.ReadOnlyMemory<byte>;
using Token = System.Threading.CancellationToken;
#endif

namespace K4os.Compression.LZ4.Streams
{
	public partial class LZ4EncoderStream
	{
		private /*async*/ void FlushStash(Token token)
		{
			var length = ClearStash();
			if (length <= 0) return;

			/*await*/ InnerWrite(token, _buffer16, 0, length);
		}

		private /*async*/ void WriteBlock(Token token, BlockInfo block)
		{
			if (!block.Ready) return;

			StashBlockLength(block);
			/*await*/ FlushStash(token);

			/*await*/ InnerWrite(token, block.Buffer, block.Offset, block.Length);

			StashBlockChecksum(block);
			/*await*/ FlushStash(token);
		}

		#if BLOCKING || NETSTANDARD2_1

		private /*async*/ void CloseFrame(Token token)
		{
			if (_encoder == null)
				return;

			var block = FlushAndEncode();
			if (block.Ready) /*await*/ WriteBlock(token, block);

			StashStreamEnd();
			/*await*/ FlushStash(token);
		}

		private /*async*/ void DisposeImpl(Token token)
		{
			/*await*/ CloseFrame(token);
			if (!_leaveOpen) /*await*/ InnerDispose(token);
		}

		#endif

		private /*async*/ void WriteImpl(Token token, ReadableBuffer buffer)
		{
			if (TryStashFrame())
				/*await*/ FlushStash(token);

			var offset = 0;
			var count = buffer.Length;

			while (count > 0)
			{
				var block = TopupAndEncode(buffer.ToSpan(), ref offset, ref count);
				if (block.Ready) /*await*/ WriteBlock(token, block);
			}
		}
	}
}
