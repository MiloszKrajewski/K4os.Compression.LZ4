#if BLOCKING
using ReadableBuffer = System.ReadOnlySpan<byte>;
using Token = K4os.Compression.LZ4.Streams.Internal.EmptyToken;
#else
using System.Threading.Tasks;
using ReadableBuffer = System.ReadOnlyMemory<byte>;
using Token = System.Threading.CancellationToken;
#endif
using K4os.Compression.LZ4.Streams.Internal;

namespace K4os.Compression.LZ4.Streams.Frames;

public partial class LZ4FrameWriter<TStreamWriter, TStreamState>
{
	private async Task WriteBlock(Token token, BlockInfo block)
	{
		if (!block.Ready) return;

		_stash.Poke4(BlockLengthCode(block));
		await FlushMeta(token).Weave();
		
		await WriteData(token, block).Weave();

		_stash.TryPoke4(BlockChecksum(block));
		await FlushMeta(token, true).Weave();
	}

	private Task WriteOneByte(Token token, byte value) =>
		WriteManyBytes(token, OneByteBuffer(token, value));

	private async Task WriteManyBytes(Token token, ReadableBuffer buffer)
	{
		if (TryStashFrame())
			await FlushMeta(token).Weave();
		
		if (_descriptor.ContentChecksum)
			UpdateContentChecksum(buffer.ToSpan());

		var offset = 0;
		var count = buffer.Length;

		while (count > 0)
		{
			var block = TopupAndEncode(buffer.ToSpan(), ref offset, ref count);
			if (block.Ready) await WriteBlock(token, block).Weave();
		}
	}

	private async Task<bool> OpenFrame(Token token)
	{
		if (!TryStashFrame())
			return false;

		await FlushMeta(token).Weave();
		return true;
	}

	private async Task CloseFrame(Token token)
	{
		if (_encoder == null)
			return;

		try
		{
			await WriteFrameTail(token);

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

	private async Task WriteFrameTail(Token token)
	{
		var block = FlushAndEncode();
		if (block.Ready)
			await WriteBlock(token, block).Weave();

		_stash.Poke4(0);
		_stash.TryPoke4(ContentChecksum());
		await FlushMeta(token, true).Weave();
	}
}