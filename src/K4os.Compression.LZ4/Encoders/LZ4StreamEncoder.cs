using System;

namespace K4os.Compression.LZ4.Encoders
{
	public unsafe class LZ4StreamEncoder
	{
		private const int Size1K = 1024;
		private const int Size64K = 0x10000;

		private byte* _inputBuffer;
		private int _inputIndex;
		private int _inputPointer;
		private int _inputLength;
		private int _blockSize;

		public LZ4StreamEncoder(int blockSize, int extraBlocks = 0)
		{
			blockSize = Math.Max(Roundup(blockSize, Size1K), Size1K);
			_blockSize = blockSize;
			_inputLength = Size64K + Math.Max(extraBlocks, 0) * blockSize + Size1K;
			_inputIndex = _inputPointer = 0;
			_inputBuffer = (byte*) Mem.Alloc(_inputLength);
		}

		private static int Roundup(int value, int step) => (value + step - 1) / step * step;

		public int Topup(byte* buffer, int length)
		{
			if (length == 0)
				return 0;

			var spaceLeft = _inputIndex + _blockSize - _inputPointer;
			if (spaceLeft <= 0)
				return 0;

			var chunk = Math.Min(spaceLeft, length);
			Mem.WildCopy(_inputBuffer, buffer, _inputBuffer + chunk);
			_inputPointer += chunk;
			return chunk;
		}
	}
}
