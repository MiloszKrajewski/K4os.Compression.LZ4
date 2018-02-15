using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using K4os.Compression.LZ4.Internal;
using LZ4EncodingContext = K4os.Compression.LZ4.Internal.LZ4_xx.LZ4_stream_t;

namespace K4os.Compression.LZ4
{
	public unsafe class LZ4Encoder: IDisposable
	{
		private readonly LZ4EncodingContext* _state;
		private readonly byte* _inputBuffer;
		private readonly byte* _outputBuffer;
		private int _disposed;
		private readonly int _blockSize;
		private readonly int _inputLength;
		private readonly int _outputLength;
		private int _blockIndex;
		private int _inputIndex;
		private int _outputIndex;

		//----

		public LZ4Encoder(int blockSize)
		{
			_state = (LZ4EncodingContext*) Mem.Alloc(sizeof(LZ4EncodingContext));
			_inputIndex = 0;
			_outputIndex = 0;
			_blockIndex = 0;
			_blockSize = Math.Max(blockSize, 1024);
			_inputLength = OptimalInputBufferSize(_blockSize);
			_outputLength = MaximumCompressedSize(_blockSize);
			_inputBuffer = (byte*) Mem.Alloc(_inputLength);
			_outputBuffer = (byte*) Mem.Alloc(_outputLength);
		}

		private static int Roundup(int value, int step) => (value + step - 1) / step * step;

		private static int OptimalInputBufferSize(int blockSize) =>
			Roundup(blockSize + Math.Max(0x10000, blockSize), blockSize);

		private static int MaximumCompressedSize(int inputSize) =>
			LZ4_xx.LZ4_compressBound(inputSize);

		public int Encode(byte* source, int sourceLength, byte* target, int targetLength)
		{
			if (Interlocked.CompareExchange(ref _disposed, 0, 0) != 0)
				throw new InvalidOperationException("Cannot use disposed encoder");

			if (sourceLength <= 0)
				return 0;

			if (sourceLength > _blockSize)
				throw new ArgumentException($"sourceLength must be smaller than {_blockSize}");

			if (_inputIndex >= _inputLength)
				_inputIndex = 0;

			var chunk = TopUp(source, sourceLength);
			if (chunk <= 0)
				return 0;

			_inputIndex += chunk;
			if (_inputIndex < _blockIndex + _blockSize)
				return 0;

			source += chunk;
			sourceLength -= chunk;

			var encoded = LZ4_64.LZ4_compress_fast_continue(
				_state,
				_inputBuffer + _inputIndex - _blockSize,
				target,
				_blockSize,
				targetLength,
				1);

			// compress
			// topup again
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
			if (_outputBuffer != null) Mem.Free(_outputBuffer);
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
