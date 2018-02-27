using System;
using K4os.Compression.LZ4.Internal;

namespace K4os.Compression.LZ4.Encoders
{
	// fast decoder context
	using LZ4Context = LZ4_xx.LZ4_streamDecode_t;

	public unsafe class LZ4StreamDecoder: LZ4Unmanaged
	{
		private readonly LZ4Context* _context;
		private readonly int _blockSize;
		private readonly byte* _outputBuffer;
		private readonly int _outputLength;
		private int _outputIndex;

		public LZ4StreamDecoder(int blockSize, int extraBlocks)
		{
			blockSize = Math.Max(Mem.RoundUp(blockSize, Mem.K1), Mem.K1);
			extraBlocks = Math.Max(extraBlocks, 0);

			_blockSize = blockSize;
			_outputLength = Mem.K64 + (1 + extraBlocks) * _blockSize + 8;
			_outputIndex = 0;
			_outputBuffer = (byte*) Mem.Alloc(_outputLength + 8);
			_context = (LZ4Context*) Mem.AllocZero(sizeof(LZ4Context));
		}

		public int Decode(byte* source, int sourceLength, byte* target, int targetLength)
		{
			Prepare();

			var decoded = DecodeBlock(source, sourceLength, _outputBuffer + _outputIndex, _blockSize);

			if (decoded < 0)
				throw new InvalidOperationException();

			if (targetLength < decoded)
				throw new InvalidOperationException();

			Mem.Copy(target, _outputBuffer + _outputIndex, decoded);
			_outputIndex += decoded;

			return decoded;
		}

		private void Prepare()
		{
			if (_outputIndex + _blockSize > _outputLength)
				_outputIndex = CopyDict(_outputBuffer, _outputIndex);
		}

		private int DecodeBlock(byte* source, int sourceLength, byte* target, int targetLength) =>
			LZ4_xx.LZ4_decompress_safe_continue(_context, source, target, sourceLength, targetLength);

		protected int CopyDict(byte* buffer, int length)
		{
			var dictStart = Math.Max(_outputIndex - length, 0);
			var dictSize = _outputIndex - dictStart;
			if (dictStart > 0)
			{
				Mem.Move(buffer, buffer + dictStart, dictSize);
				LZ4_xx.LZ4_setStreamDecode(_context, buffer, dictSize);
			}

			return dictSize;
		}

		protected override void ReleaseUnmanaged()
		{
			base.ReleaseUnmanaged();
			Mem.Free(_context);
			Mem.Free(_outputBuffer);
		}
	}
}
