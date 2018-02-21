using System;
using System.Threading;
using K4os.Compression.LZ4.Internal;

namespace K4os.Compression.LZ4.Encoders
{
	public abstract unsafe class LZ4AbstractEncoder: IDisposable
	{
		private const int MinBlockSize = 0x10000;
		private readonly byte* _inputBuffer;
		private int _disposed;
		private readonly int _blockSize;
		private readonly int _inputLength;
		private int _blockIndex;
		private int _inputIndex;

		protected LZ4AbstractEncoder(int blockSize)
		{
			_inputIndex = 0;
			_blockIndex = 0;
			_blockSize = Roundup(Math.Max(blockSize, MinBlockSize), 1024);
			_inputLength = 2 * _blockSize;
			_inputBuffer = (byte*) Mem.Alloc(_inputLength);
		}

		private static int Roundup(int value, int step) => (value + step - 1) / step * step;

		private static int MaximumCompressedSize(int inputSize) =>
			LZ4_xx.LZ4_compressBound(inputSize);

		private static void Validate(byte[] buffer, int index, int length)
		{
			if (buffer == null) throw new ArgumentNullException(nameof(buffer), "cannot be null");

			var valid = index >= 0 && length >= 0 && index + length <= buffer.Length;
			if (!valid) throw new ArgumentException($"invald index/length combination: {index}/{length}");
		}

		public int Encode(
			byte[] source, int sourceIndex, int sourceLength,
			byte[] target, int targetIndex, int targetLength)
		{
			Validate(source, sourceIndex, sourceLength);
			Validate(target, targetIndex, targetLength);
			fixed (byte* sourceP = &source[sourceIndex])
			fixed (byte* targetP = &target[targetIndex])
				return Encode(sourceP, sourceLength, targetP, targetLength);
		}

		public int Encode(byte* source, int sourceLength, byte* target, int targetLength)
		{
			if (sourceLength <= 0)
				return 0;

			if (Interlocked.CompareExchange(ref _disposed, 0, 0) != 0)
				throw new InvalidOperationException("Cannot use disposed encoder");

			if (sourceLength > _blockSize)
				throw new ArgumentException($"sourceLength must be smaller than {_blockSize}");

			if (targetLength < _blockSize)
				throw new ArgumentException($"targetLength must be at least equal to {_blockSize}");

			var chunk = TopUp(source, sourceLength);
			if (chunk <= 0)
				return 0;

			_inputIndex += chunk;
			if (_inputIndex < _blockIndex + _blockSize)
				return 0;

			source += chunk;
			sourceLength -= chunk;

			var encoded = EncodeBlock(_inputBuffer + _blockIndex, _blockSize, target, targetLength);

			if (_inputIndex >= _inputLength)
				_inputIndex = 0;

			_blockIndex = _inputIndex;

			_inputIndex += TopUp(source, sourceLength);

			return encoded;
		}

		protected abstract int EncodeBlock(
			byte* source, int sourceLength, byte* target, int targetLength);

		private int TopUp(byte* source, int sourceLength)
		{
			if (sourceLength <= 0)
				return 0;

			var chunk = Math.Min(sourceLength, _blockIndex + _blockSize - _inputIndex);
			Mem.Copy(_inputBuffer + _inputIndex, source, chunk);
			return chunk;
		}

		protected virtual void DisposeManaged() { }

		protected virtual void DisposeUnmanaged()
		{
			Mem.Free(_inputBuffer);
		}

		public void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		private void Dispose(bool disposing)
		{
			if (Interlocked.CompareExchange(ref _disposed, 1, 0) != 0)
				return;

			DisposeUnmanaged();
			if (disposing)
				DisposeManaged();
		}

		~LZ4AbstractEncoder()
		{
			Dispose(false);
		}
	}
}
