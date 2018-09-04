using System;
using K4os.Compression.LZ4.Internal;

namespace K4os.Compression.LZ4.Encoders
{
	public abstract unsafe class LZ4EncoderBase: UnmanagedResources, ILZ4Encoder
	{
		private readonly byte* _inputBuffer;
		private readonly int _inputLength;
		private readonly int _blockSize;

		private int _inputIndex;
		private int _inputPointer;
		private int _encodedLimit;

		protected LZ4EncoderBase(int blockSize, int extraBlocks = 0)
		{
			blockSize = Mem.RoundUp(Math.Max(blockSize, Mem.K1), Mem.K1);
			extraBlocks = Math.Max(extraBlocks, 0);

			_blockSize = blockSize;
			_encodedLimit = LZ4Codec.MaximumOutputSize(blockSize);
			_inputLength = Mem.K64 + (1 + extraBlocks) * blockSize + 8;
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
			Mem.Move(_inputBuffer + _inputPointer, source, chunk);
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

			if (encoded <= 0)
				throw new InvalidOperationException(
					"Failed to compress chunk. Encoder has been corrupted.");

			Commit();

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

			Mem.Move(target, _inputBuffer + _inputIndex, sourceLength);

			Commit();

			return sourceLength;
		}

		private void Commit()
		{
			_inputIndex = _inputPointer;
			if (_inputIndex + _blockSize <= _inputLength)
				return;

			_inputIndex = _inputPointer = CopyDict(_inputBuffer, _inputPointer);
		}

		protected abstract int EncodeBlock(
			byte* source, int sourceLength, byte* target, int targetLength);

		protected abstract int CopyDict(byte* buffer, int dictionaryLength);

		protected override void ReleaseUnmanaged()
		{
			base.ReleaseUnmanaged();
			Mem.Free(_inputBuffer);
		}
	}
}
