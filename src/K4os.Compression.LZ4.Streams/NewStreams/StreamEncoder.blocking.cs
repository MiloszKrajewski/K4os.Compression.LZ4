//------------------------------------------------------------------------------
//
// This file has been generated. All changes will be lost.
//
//------------------------------------------------------------------------------
#define BLOCKING

using System;
using K4os.Compression.LZ4.Streams.Internal;

#if BLOCKING
using ReadableBuffer = System.ReadOnlySpan<byte>;
using Token = K4os.Compression.LZ4.Streams.Internal.EmptyToken;
#else
using System.Threading.Tasks;
using ReadableBuffer = System.ReadOnlyMemory<byte>;
using Token = System.Threading.CancellationToken;
#endif

namespace K4os.Compression.LZ4.Streams.NewStreams
{
	public partial class StreamEncoder<TStream> where TStream: IStreamWriter
	{
		private /*async*/ void WriteBlockImpl(Token token, BlockInfo block)
		{
			if (!block.Ready) return;

			Tools.Stash4(BlockLengthCode(block));
			/*await*/ Tools.Flush(token);

			/*await*/ InnerWriteBlock(token, block.Buffer, block.Offset, block.Length);

			Tools.TryStash4(BlockChecksum(block));
			/*await*/ Tools.Flush(token);
		}

		private /*async*/ void WriteBytesImpl(Token token, ReadableBuffer buffer)
		{
			if (TryStashFrame())
				/*await*/ Tools.Flush(token);

			var offset = 0;
			var count = buffer.Length;

			while (count > 0)
			{
				var block = TopupAndEncode(buffer.ToSpan(), ref offset, ref count);
				if (block.Ready) /*await*/ WriteBlockImpl(token, block);
			}
		}
		
		private /*async*/ void CloseFrameImpl(Token token)
		{
			if (_encoder == null)
				return;

			var block = FlushAndEncode();
			if (block.Ready) /*await*/ WriteBlockImpl(token, block);

			Tools.Stash4(0);
			Tools.TryStash4(ContentChecksum());
			/*await*/ Tools.Flush(token);
		}
	}
}
