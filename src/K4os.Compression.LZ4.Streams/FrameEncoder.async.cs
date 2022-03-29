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

namespace K4os.Compression.LZ4.Streams
{
	public partial class FrameEncoder<TStream>
	{
		private async Task WriteBlock(Token token, BlockInfo block)
		{
			if (!block.Ready) return;

			Writer.Poke4(BlockLengthCode(block));
			await Writer.Flush(token).Weave();

			await Writer.Write(token, block.Buffer, block.Offset, block.Length).Weave();

			Writer.TryPoke4(BlockChecksum(block));
			await Writer.Flush(token).Weave();
		}

		private Task WriteOneByte(Token token, byte value) =>
			WriteManyBytes(token, Writer.OneByteBuffer(token, value));

		private async Task WriteManyBytes(Token token, ReadableBuffer buffer)
		{
			if (TryStashFrame())
				await Writer.Flush(token).Weave();

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

			await Writer.Flush(token).Weave();
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

			Writer.Poke4(0);
			Writer.TryPoke4(ContentChecksum());
			await Writer.Flush(token).Weave();
		}
	}
}
