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
		private async Task WriteBlock(Token token, BlockInfo block)
		{
			if (!block.Ready) return;

			Stash.Stash4(BlockLengthCode(block));
			await Stash.FlushWrite(token).Weave();

			await InnerWrite(token, block.Buffer, block.Offset, block.Length).Weave();

			Stash.TryStash4(BlockChecksum(block));
			await Stash.FlushWrite(token).Weave();
		}

		#if BLOCKING || NETSTANDARD2_1

		private async Task CloseFrame(Token token)
		{
			if (_encoder == null)
				return;

			var block = FlushAndEncode();
			if (block.Ready) await WriteBlock(token, block).Weave();

			Stash.Stash4(0);
			Stash.TryStash4(ContentChecksum());
			await Stash.FlushWrite(token).Weave();
		}

		private async Task DisposeImpl(Token token)
		{
			await CloseFrame(token).Weave();
			await InnerDispose(token, false).Weave();
		}

		#endif

		private async Task WriteImpl(Token token, ReadableBuffer buffer)
		{
			if (TryStashFrame())
				await Stash.FlushWrite(token).Weave();

			var offset = 0;
			var count = buffer.Length;

			while (count > 0)
			{
				var block = TopupAndEncode(buffer.ToSpan(), ref offset, ref count);
				if (block.Ready) await WriteBlock(token, block).Weave();
			}
		}
	}
}
