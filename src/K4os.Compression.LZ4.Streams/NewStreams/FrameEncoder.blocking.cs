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
	public partial class FrameEncoder<TStream> where TStream: IStreamWriter
	{
		private /*async*/ void WriteBlock(Token token, BlockInfo block)
		{
			if (!block.Ready) return;

			Writer.Poke4(BlockLengthCode(block));
			/*await*/ Writer.Flush(token);
			
			Writer.Write(token, block.Buffer, block.Offset, block.Length);

			Writer.TryPoke4(BlockChecksum(block));
			/*await*/ Writer.Flush(token);
		}
		
		private void WriteOneByte(Token token, byte value) => 
			WriteManyBytes(token, Writer.OneByteBuffer(token, value));

		private /*async*/ void WriteManyBytes(Token token, ReadableBuffer buffer)
		{
			if (TryStashFrame())
				/*await*/ Writer.Flush(token);

			var offset = 0;
			var count = buffer.Length;

			while (count > 0)
			{
				var block = TopupAndEncode(buffer.ToSpan(), ref offset, ref count);
				if (block.Ready) /*await*/ WriteBlock(token, block);
			}
		}
		
		private /*async*/ void CloseFrame(Token token)
		{
			if (_encoder == null)
				return;

			var block = FlushAndEncode();
			if (block.Ready) 
				/*await*/ WriteBlock(token, block);

			Writer.Poke4(0);
			Writer.TryPoke4(ContentChecksum());
			/*await*/ Writer.Flush(token);
		}
	}
}
