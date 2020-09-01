//------------------------------------------------------------------------------
//
// This file has been generated. All changes will be lost.
//
//------------------------------------------------------------------------------
#define BLOCKING

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
		private void InnerFlush() =>
			_inner.Flush();

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private void InnerWrite(
			byte[] buffer, int offset, int length) =>
			_inner.Write(buffer, offset, length);

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private void InnerWrite(
			BlockInfo block) =>
			InnerWrite(block.Buffer, block.Offset, block.Length);

		private /*async*/ void FlushStash()
		{
			var length = ClearStash();
			if (length <= 0) return;

			/*await*/ InnerWrite(_buffer16, 0, length);
		}

		private /*async*/ void WriteBlock(BlockInfo block)
		{
			if (!block.Ready) return;

			StashBlockLength(block);
			/*await*/ FlushStash();

			/*await*/ InnerWrite(block);

			StashBlockChecksum(block);
			/*await*/ FlushStash();
		}

		#if BLOCKING || NETSTANDARD2_1

		private /*async*/ void CloseFrame()
		{
			if (_encoder == null)
				return;

			var block = FlushAndEncode();
			if (block.Ready) /*await*/ WriteBlock(block);

			StashStreamEnd();
			/*await*/ FlushStash();
		}

		private /*async*/ void DisposeImpl()
		{
			/*await*/ CloseFrame();
			if (!_leaveOpen)
				/*await*/ _inner.Dispose();
		}

		#endif

		private /*async*/ void WriteImpl(ReadableBuffer buffer)
		{
			if (TryStashFrame())
				/*await*/ FlushStash();

			var offset = 0;
			var count = buffer.Length;

			while (count > 0)
			{
				var block = TopupAndEncode(ToSpan(buffer), ref offset, ref count);
				if (block.Ready) /*await*/ WriteBlock(block);
			}
		}
	}
}
