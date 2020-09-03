using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace K4os.Compression.LZ4.Streams
{
	internal struct Stash
	{
		private readonly Stream _stream;
		private readonly byte[] buffer;
		private int head;
		private readonly int size;

		public Stash(Stream innerStream, int bufferSize)
		{
			_stream = innerStream;
			size = bufferSize;
			buffer = new byte[bufferSize + 8];
			head = 0;
		}

		public bool Empty => head <= 0;

		public int Length => head;

		public int Clear()
		{
			var result = head;
			head = 0;
			return result;
		}

		public void FlushWrite(EmptyToken token)
		{
			var length = Clear();
			if (length <= 0) return;

			_stream.Write(buffer, 0, length);
		}

		public Task FlushWrite(CancellationToken token)
		{
			var length = Clear();
			return length <= 0 
				? LZ4Stream.CompletedTask 
				: _stream.WriteAsync(buffer, 0, length, token);
		}
		
		public void Stash1(byte value)
		{
			buffer[head + 0] = value;
			head++;
		}
		
		public void Stash2(ushort value)
		{
			buffer[head + 0] = (byte) (value >> 0);
			buffer[head + 1] = (byte) (value >> 8);
			head += 2;
		}

		public void Stash4(uint value)
		{
			buffer[head + 0] = (byte) (value >> 0);
			buffer[head + 1] = (byte) (value >> 8);
			buffer[head + 2] = (byte) (value >> 16);
			buffer[head + 3] = (byte) (value >> 24);
			head += 4;
		}
		
		public void TryStash4(uint? value)
		{
			if (!value.HasValue) return;
			Stash4(value.Value);
		}

		public void Stash8(ulong value)
		{
			buffer[head + 0] = (byte) (value >> 0);
			buffer[head + 1] = (byte) (value >> 8);
			buffer[head + 2] = (byte) (value >> 16);
			buffer[head + 3] = (byte) (value >> 24);
			buffer[head + 4] = (byte) (value >> 32);
			buffer[head + 5] = (byte) (value >> 40);
			buffer[head + 6] = (byte) (value >> 48);
			buffer[head + 7] = (byte) (value >> 56);
			head += 8;
		}

		public Span<byte> AsSpan(int offset = 0) => 
			buffer.AsSpan(offset, Math.Max(0, head - offset));

		public Span<byte> OneByte(in byte value)
		{
			buffer[size] = value;
			return buffer.AsSpan(size, 1);
		}
	}
}
