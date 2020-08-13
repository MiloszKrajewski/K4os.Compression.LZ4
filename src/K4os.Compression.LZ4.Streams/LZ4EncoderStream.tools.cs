using System.Threading;
using System.Threading.Tasks;
using K4os.Compression.LZ4.Internal;

namespace K4os.Compression.LZ4.Streams
{
	public partial class LZ4EncoderStream
	{
		// ReSharper disable once InconsistentNaming
		private const int _length16 = 16;
		private readonly byte[] _buffer16 = new byte[_length16 + 8];
		private int _index16;
		
		private int MaxBlockSizeCode(int blockSize) =>
			blockSize <= Mem.K64 ? 4 :
			blockSize <= Mem.K256 ? 5 :
			blockSize <= Mem.M1 ? 6 :
			blockSize <= Mem.M4 ? 7 :
			throw InvalidBlockSize(blockSize);

		private void Stash1(byte value)
		{
			_buffer16[_index16 + 0] = value;
			_index16++;
		}

		private void Stash2(ushort value)
		{
			_buffer16[_index16 + 0] = (byte) (value >> 0);
			_buffer16[_index16 + 1] = (byte) (value >> 8);
			_index16 += 2;
		}

		private void Stash4(uint value)
		{
			_buffer16[_index16 + 0] = (byte) (value >> 0);
			_buffer16[_index16 + 1] = (byte) (value >> 8);
			_buffer16[_index16 + 2] = (byte) (value >> 16);
			_buffer16[_index16 + 3] = (byte) (value >> 24);
			_index16 += 4;
		}

		/*
		private void Stash8(ulong value)
		{
		    _buffer16[_index16 + 0] = (byte) (value >> 0);
		    _buffer16[_index16 + 1] = (byte) (value >> 8);
		    _buffer16[_index16 + 2] = (byte) (value >> 16);
		    _buffer16[_index16 + 3] = (byte) (value >> 24);
		    _buffer16[_index16 + 4] = (byte) (value >> 32);
		    _buffer16[_index16 + 5] = (byte) (value >> 40);
		    _buffer16[_index16 + 6] = (byte) (value >> 48);
		    _buffer16[_index16 + 7] = (byte) (value >> 56);
		    _index16 += 8;
		}
		*/
		
		private int ClearStash()
		{
			var length = _index16;
			_index16 = 0;
			return length;
		}

		private void FlushStash()
		{
			var length = ClearStash();
			if (length <= 0) return;

			_inner.Write(_buffer16, 0, length);
		}

		private Task FlushStashAsync(CancellationToken token)
		{
			var length = ClearStash();
			if (length <= 0) return LZ4Stream.CompletedTask;

			return _inner.WriteAsync(_buffer16, 0, length, token);
		}

		private void InnerWrite(BlockInfo block)
		{
			if (_index16 > 0) FlushStash();
			if (block.Ready) 
				_inner.Write(block.Buffer, block.Offset, block.Length);
		}
		
		private async Task InnerWriteAsync(BlockInfo block, CancellationToken token)
		{
			if (_index16 > 0) await FlushStashAsync(token);
			if (block.Ready) 
				await _inner.WriteAsync(
					block.Buffer, block.Offset, block.Length, token);
		}
	}
}
