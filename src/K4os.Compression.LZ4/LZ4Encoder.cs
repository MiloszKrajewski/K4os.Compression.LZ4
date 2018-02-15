using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using K4os.Compression.LZ4.Internal;

namespace K4os.Compression.LZ4
{
	public unsafe class LZ4Encoder: IDisposable
	{
		private readonly LZ4_xx.LZ4_stream_t* _state;
		private readonly byte* _inputBuffer;
		private readonly byte* _outputBuffer;
		private readonly int _maxBlockLength;
		private int _disposed;
		private readonly int _inputLength;
		private readonly int _outputLength;
		private int _inputIndex;
		private int _outputIndex;

		//----

		public LZ4Encoder(int blockLength)
		{
			_state = (LZ4_xx.LZ4_stream_t*) Mem.Alloc(sizeof(LZ4_xx.LZ4_stream_t));
			_inputIndex = 0;
			_outputIndex = 0;
			_maxBlockLength = Math.Max(blockLength, 1024);
			_inputLength = _maxBlockLength + 0x10000;
			_outputLength = LZ4_xx.LZ4_compressBound(_maxBlockLength);
			_inputBuffer = (byte*) Mem.Alloc(_inputLength);
			_outputBuffer = (byte*) Mem.Alloc(_outputLength);
		}

		public int Encode(byte* source, int sourceLength, byte* target, int targetLength)
		{
			if (Interlocked.CompareExchange(ref _disposed, 0, 0) != 0)
				throw new InvalidOperationException("Cannot use disposed encoder");

			if (sourceLength <= 0)
				return 0;

			if (sourceLength > _maxBlockLength)
				throw new ArgumentException($"sourceLength must be smaller than {_maxBlockLength}");

			// top-up
			// compress

			var blockStrart = _inputIndex & _maxBlockLength;

			var topup = Math.Min(Math.Min(_blockLeft, _inputLength - _inputIndex), sourceLength);
			if (topup > 0)
			{
				Mem.Copy(_inputBuffer + _inputIndex, source, topup);
				_inputIndex += topup;
				source += topup;
				_blockLeft -= topup;
			}

			
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
