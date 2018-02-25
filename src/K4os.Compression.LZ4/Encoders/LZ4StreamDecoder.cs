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
			_blockSize = Math.Max(Mem.RoundUp(blockSize, Mem.K1), Mem.K1);
			_outputLength = Mem.K64 + (1 + Math.Max(extraBlocks, 0)) * _blockSize + 8;
			_outputIndex = 0;
			_outputBuffer = (byte*) Mem.AllocZero(_outputLength + 8);
			_context = (LZ4Context*) Mem.AllocZero(sizeof(LZ4Context));
		}

		public int Decode(byte* source, int sourceLength, byte* target, int targetLength)
		{
			AdjustBlockStart();

			var decoded = LZ4_xx.LZ4_decompress_safe_continue(
				_context,
				source,
				_outputBuffer + _outputIndex,
				sourceLength,
				_blockSize);

			if (decoded < 0)
				throw new InvalidOperationException();

			if (targetLength < decoded)
				throw new InvalidOperationException();

			Mem.Copy(target, _outputBuffer + _outputIndex, decoded);
			_outputIndex += decoded;

			return decoded;
		}

		private void AdjustBlockStart()
		{
			if (_outputIndex + _blockSize <= _outputLength)
				return;

			var actualSize = Mem.WildShift0(_outputBuffer, ref _outputIndex, Mem.K64);
			SetupPrefix(_outputBuffer + _outputIndex - actualSize, actualSize);
		}

		protected void SetupPrefix(byte* b, int dictionaryLength) { }

		protected override void ReleaseUnmanaged()
		{
			base.ReleaseUnmanaged();
			Mem.Free(_context);
			Mem.Free(_outputBuffer);
		}
	}
}
