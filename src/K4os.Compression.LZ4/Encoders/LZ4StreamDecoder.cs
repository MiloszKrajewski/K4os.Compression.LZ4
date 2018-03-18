using System;
using K4os.Compression.LZ4.Internal;

namespace K4os.Compression.LZ4.Encoders
{
	// fast decoder context
	using LZ4Context = LZ4_xx.LZ4_streamDecode_t;

	public unsafe class LZ4StreamDecoder: UnmanagedResources, ILZ4StreamDecoder
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

		public int BlockSize => _blockSize;
		public int BytesReady => _outputIndex;

		public int Decode(byte* source, int length)
		{
			Prepare();

			var decoded = DecodeBlock(source, length, _outputBuffer + _outputIndex, _blockSize);

			if (decoded < 0)
				throw new InvalidOperationException();

			_outputIndex += decoded;

			return decoded;
		}

		public void Drain(byte* target, int offset, int length)
		{
			offset = _outputIndex + offset; // NOTE: negative value
			if (offset < 0 || length < 0 || offset + length > _outputIndex)
				throw new InvalidOperationException();
			Mem.Copy(target, _outputBuffer + offset, length);
		}

		private void Prepare()
		{
			if (_outputIndex + _blockSize <= _outputLength)
				return;

			var dictStart = Math.Max(_outputIndex - _blockSize, 0);
			var dictSize = _outputIndex - dictStart;
			Mem.Copy(_outputBuffer, _outputBuffer + dictStart, dictSize);
			LZ4_xx.LZ4_setStreamDecode(_context, _outputBuffer, dictSize);
			_outputIndex = dictSize;
		}

		private int DecodeBlock(byte* source, int sourceLength, byte* target, int targetLength) =>
			LZ4_xx.LZ4_decompress_safe_continue(_context, source, target, sourceLength, targetLength);

		protected override void ReleaseUnmanaged()
		{
			base.ReleaseUnmanaged();
			Mem.Free(_context);
			Mem.Free(_outputBuffer);
		}
	}
}
