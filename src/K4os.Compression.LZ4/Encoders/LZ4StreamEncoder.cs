using System;
using System.Threading;

namespace K4os.Compression.LZ4.Encoders
{
	public abstract unsafe class LZ4StreamEncoder: ILZ4StreamEncoder
	{
		private const int Size1K = 1024;
		private const int Size64K = 0x10000;

		private int _disposed;

		private readonly byte* _inputBuffer;
		private readonly int _inputLength;
		private readonly int _blockSize;

		private int _inputIndex;
		private int _inputPointer;

		protected LZ4StreamEncoder(int blockSize, int extraBlocks = 0)
		{
			_blockSize = Math.Max(Roundup(blockSize, Size1K), Size1K);
			_inputLength = Size64K + (1 + Math.Max(extraBlocks, 0)) * _blockSize + 8;
			_inputIndex = _inputPointer = 0;
			_inputBuffer = (byte*) Mem.Alloc(_inputLength + 8);
		}

		private static int Roundup(int value, int step) => (value + step - 1) / step * step;

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
			Mem.WildCopy(_inputBuffer, source, _inputBuffer + chunk);
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

			var dictionaryIndex = Math.Max(_inputPointer - Size64K, 0) & ~0x7;
			var dictionaryLength = _inputPointer - dictionaryIndex;
			Mem.WildCopy(_inputBuffer, _inputBuffer + dictionaryIndex, _inputBuffer + dictionaryLength);
			_inputPointer = _inputIndex = dictionaryLength;
			dictionaryLength = Math.Min(dictionaryLength, Size64K);

			SetupPrefix(_inputBuffer + _inputPointer - dictionaryLength, dictionaryLength);
		}

		protected abstract int EncodeBlock(
			byte* source, int sourceLength, byte* target, int targetLength);

		protected abstract void SetupPrefix(byte* dictionary, int dictionaryLength);

		private void ThrowIfDisposed()
		{
			if (Interlocked.CompareExchange(ref _disposed, 0, 0) != 0)
				throw new InvalidOperationException();
		}

		protected virtual void ReleaseUnmanaged()
		{
			Mem.Free(_inputBuffer);
		}

		protected virtual void ReleaseManaged() { }

		protected virtual void Dispose(bool disposing)
		{
			if (Interlocked.CompareExchange(ref _disposed, 1, 0) != 0)
				return;

			ReleaseUnmanaged();
			if (disposing)
				ReleaseManaged();
		}

		public void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		~LZ4StreamEncoder()
		{
			Dispose(false);
		}
	}
}
