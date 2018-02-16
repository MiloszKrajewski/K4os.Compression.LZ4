using System;
using System.Threading;
using K4os.Compression.LZ4.Internal;
using LZ4EncodingContext = K4os.Compression.LZ4.Internal.LZ4_xx.LZ4_stream_t;

namespace K4os.Compression.LZ4
{
	public unsafe class LZ4Encoder: IDisposable
	{
		private readonly LZ4EncodingContext* _state;
		private readonly byte* _inputBuffer;
		private int _disposed;
		private readonly int _blockSize;
		private readonly int _inputLength;
		private int _blockIndex;
		private int _inputIndex;

		public LZ4Encoder(int blockSize)
		{
			_state = (LZ4EncodingContext*) Mem.Alloc(sizeof(LZ4EncodingContext));
			_inputIndex = 0;
			_blockIndex = 0;
			_blockSize = Math.Max(blockSize, 1024);
			_inputLength = OptimalInputBufferSize(_blockSize);
			_inputBuffer = (byte*) Mem.Alloc(_inputLength);
		}

		private static int Roundup(int value, int step) => (value + step - 1) / step * step;

		private static int OptimalInputBufferSize(int blockSize) =>
			Roundup(blockSize + Math.Max(0x10000, blockSize), blockSize);

		private static int MaximumCompressedSize(int inputSize) =>
			LZ4_xx.LZ4_compressBound(inputSize);

		private void Validate(byte[] buffer, int bufferIndex, int bufferLength)
		{
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

		protected virtual int EncodeBlock(byte* source, int sourceLength, byte* target, int targetLength)
		{
			Mem.Copy(target, source, sourceLength);
			return targetLength;
		}

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
			if (Interlocked.CompareExchange(ref _disposed, 1, 0) != 0)
				return;

			if (_state != null) Mem.Free(_state);
			if (_inputBuffer != null) Mem.Free(_inputBuffer);
		}

		public void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		private void Dispose(bool disposing)
		{
			DisposeUnmanaged();
			if (disposing)
				DisposeManaged();
		}

		~LZ4Encoder()
		{
			Dispose(false);
		}
	}
}
