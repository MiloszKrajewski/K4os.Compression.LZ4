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
		private /*async*/ void WriteBlock(Token token, BlockInfo block)
		{
			if (!block.Ready) return;

			Stash.Stash4(BlockLengthCode(block));
			/*await*/ Stash.FlushWrite(token);

			/*await*/ InnerWrite(token, block.Buffer, block.Offset, block.Length);

			Stash.TryStash4(BlockChecksum(block));
			/*await*/ Stash.FlushWrite(token);
		}

		#if BLOCKING || NETSTANDARD2_1

		private /*async*/ void CloseFrame(Token token)
		{
			if (_encoder == null)
				return;

			var block = FlushAndEncode();
			if (block.Ready) /*await*/ WriteBlock(token, block);

			Stash.Stash4(0);
			Stash.TryStash4(ContentChecksum());
			/*await*/ Stash.FlushWrite(token);
		}

		private /*async*/ void DisposeImpl(Token token)
		{
			/*await*/ CloseFrame(token);
			/*await*/ InnerDispose(token, false);
		}

		#endif

		private /*async*/ void WriteImpl(Token token, ReadableBuffer buffer)
		{
			if (TryStashFrame())
				/*await*/ Stash.FlushWrite(token);

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
