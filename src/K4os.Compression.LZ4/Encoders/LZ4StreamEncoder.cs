using System;
using K4os.Compression.LZ4.Internal;

namespace K4os.Compression.LZ4.Encoders
{
	public abstract unsafe class LZ4StreamEncoder: LZ4Unmanaged, ILZ4StreamEncoder
	{
		private readonly byte* _inputBuffer;
		private readonly int _inputLength;
		private readonly int _blockSize;

		private int _inputIndex;
		private int _inputPointer;

		protected LZ4StreamEncoder(int blockSize, int extraBlocks = 0)
		{
			_blockSize = Math.Max(Mem.RoundUp(blockSize, Mem.K1), Mem.K1);
			_inputLength = Mem.K64 + (1 + Math.Max(extraBlocks, 0)) * _blockSize + 8;
			_inputIndex = _inputPointer = 0;
			_inputBuffer = (byte*) Mem.Alloc(_inputLength + 8);
		}

		public int BlockSize => _blockSize;
		public int BytesReady => _inputPointer - _inputIndex;

		public int Topup(byte* source, int length)
		{
			ThrowIfDisposed();

			if (length == 0)
				return 0;

			var spaceLeft = _inputIndex + _blockSize - _inputPointer;
			if (spaceLeft <= 0)
				return 0;

			var chunk = Math.Min(spaceLeft, length);
			Mem.Copy(_inputBuffer, source, chunk);
			_inputPointer += chunk;
			return chunk;
		}

		public int Encode(byte* target, int length)
		{
			ThrowIfDisposed();

			var sourceLength = _inputPointer - _inputIndex;
			if (sourceLength <= 0)
				return 0;

			var encoded = EncodeBlock(_inputBuffer + _inputIndex, sourceLength, target, length);

			if (encoded < 0)
				throw new InvalidOperationException();

			if (encoded == 0)
				return 0;

			AdjustBlockStart();

			return encoded;
		}

		public int Copy(byte* target, int length)
		{
			ThrowIfDisposed();

			var sourceLength = _inputPointer - _inputIndex;
			if (sourceLength <= 0)
				return 0;

			if (length < sourceLength)
				throw new InvalidOperationException();

			Mem.Copy(target, _inputBuffer + _inputIndex, sourceLength);

			AdjustBlockStart();

			return sourceLength;
		}

		private void AdjustBlockStart()
		{
			_inputIndex = _inputPointer;
			if (_inputIndex + _blockSize <= _inputLength)
				return;

			var dictionaryIndex = Math.Max(_inputPointer - Mem.K64, 0) & ~0x7;
			var dictionaryLength = _inputPointer - dictionaryIndex;
			Mem.WildCopy(_inputBuffer, _inputBuffer + dictionaryIndex, _inputBuffer + dictionaryLength);
			_inputPointer = _inputIndex = dictionaryLength;
			dictionaryLength = Math.Min(dictionaryLength, Mem.K64);

			SetupPrefix(_inputBuffer + _inputPointer - dictionaryLength, dictionaryLength);
		}

		protected abstract int EncodeBlock(
			byte* source, int sourceLength, byte* target, int targetLength);

		protected abstract void SetupPrefix(byte* dictionary, int dictionaryLength);

		protected override void ReleaseUnmanaged()
		{
			base.ReleaseUnmanaged();
			Mem.Free(_inputBuffer);
		}
	}
}
