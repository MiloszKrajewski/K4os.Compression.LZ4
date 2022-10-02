//------------------------------------------------------------------------------
//
// This file has been generated. All changes will be lost.
//
//------------------------------------------------------------------------------
#define BLOCKING

#if BLOCKING
using ReadableBuffer = System.ReadOnlySpan<byte>;
using Token = K4os.Compression.LZ4.Streams.Internal.EmptyToken;
#else
using System.Threading.Tasks;
using ReadableBuffer = System.ReadOnlyMemory<byte>;
using Token = System.Threading.CancellationToken;
#endif
using System;
using K4os.Compression.LZ4.Streams.Internal;

namespace K4os.Compression.LZ4.Streams.Frames;

public partial class FrameEncoder<TStreamWriter, TStreamState>
{
	private /*async*/ void WriteBlock(Token token, BlockInfo block)
	{
		if (!block.Ready) return;

		_stash.Poke4(BlockLengthCode(block));
		/*await*/ FlushMeta(token);
		
		/*await*/ WriteData(token, block);

		_stash.TryPoke4(BlockChecksum(block));
		/*await*/ FlushMeta(token);
	}

	private void WriteOneByte(Token token, byte value) =>
		WriteManyBytes(token, OneByteBuffer(token, value));

	private /*async*/ void WriteManyBytes(Token token, ReadableBuffer buffer)
	{
		if (TryStashFrame())
			/*await*/ FlushMeta(token);

		var offset = 0;
		var count = buffer.Length;

		while (count > 0)
		{
			var block = TopupAndEncode(buffer.ToSpan(), ref offset, ref count);
			if (block.Ready) /*await*/ WriteBlock(token, block);
		}
	}

	private /*async*/ bool OpenFrame(Token token)
	{
		if (!TryStashFrame())
			return false;

		/*await*/ FlushMeta(token);
		return true;
	}

	private /*async*/ void CloseFrame(Token token)
	{
		if (_encoder == null)
			return;

		try
		{
			/*await*/ WriteFrameTail(token);

			if (_buffer is not null)
				ReleaseBuffer(_buffer);
			_encoder.Dispose();
		}
		finally
		{
			_encoder = null;
			_descriptor = null;
			_buffer = null;
		}
	}

	private /*async*/ void WriteFrameTail(Token token)
	{
		var block = FlushAndEncode();
		if (block.Ready)
			/*await*/ WriteBlock(token, block);

		_stash.Poke4(0);
		_stash.TryPoke4(ContentChecksum());
		/*await*/ FlushMeta(token);
	}
}