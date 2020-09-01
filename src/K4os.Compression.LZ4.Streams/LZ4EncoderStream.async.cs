using System;
using System.Runtime.CompilerServices;

#if BLOCKING
using ReadableBuffer = System.ReadOnlySpan<byte>;
#else
using System.Threading;
using System.Threading.Tasks;
using ReadableBuffer = System.ReadOnlyMemory<byte>;
#endif

namespace K4os.Compression.LZ4.Streams
{
	public partial class LZ4EncoderStream
	{
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private Task InnerFlushAsync(in CancellationToken token) =>
			_inner.FlushAsync(token);

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private Task InnerWriteAsync(
			in CancellationToken token, byte[] buffer, int offset, int length) =>
			_inner.WriteAsync(buffer, offset, length, token);

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private Task InnerWriteAsync(
			in CancellationToken token, BlockInfo block) =>
			InnerWriteAsync(token, block.Buffer, block.Offset, block.Length);

		private async Task FlushStashAsync(CancellationToken token)
		{
			var length = ClearStash();
			if (length <= 0) return;

			await InnerWriteAsync(token, _buffer16, 0, length);
		}

		private async Task WriteBlockAsync(CancellationToken token, BlockInfo block)
		{
			if (!block.Ready) return;

			StashBlockLength(block);
			await FlushStashAsync(token);

			await InnerWriteAsync(token, block);

			StashBlockChecksum(block);
			await FlushStashAsync(token);
		}

		#if BLOCKING || NETSTANDARD2_1

		private async Task CloseFrameAsync(CancellationToken token)
		{
			if (_encoder == null)
				return;

			var block = FlushAndEncode();
			if (block.Ready) await WriteBlockAsync(token, block);

			StashStreamEnd();
			await FlushStashAsync(token);
		}

		private async Task DisposeImplAsync(CancellationToken token)
		{
			await CloseFrameAsync(token);
			if (!_leaveOpen)
				await _inner.DisposeAsync();
		}

		#endif

		private async Task WriteImplAsync(CancellationToken token, ReadableBuffer buffer)
		{
			if (TryStashFrame())
				await FlushStashAsync(token);

			var offset = 0;
			var count = buffer.Length;

			while (count > 0)
			{
				var block = TopupAndEncode(ToSpan(buffer), ref offset, ref count);
				if (block.Ready) await WriteBlockAsync(token, block);
			}
		}
	}
}
