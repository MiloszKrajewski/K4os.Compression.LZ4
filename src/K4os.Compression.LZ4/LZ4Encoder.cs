using System;
using System.Collections.Generic;
using System.Threading;
using K4os.Compression.LZ4.Internal;

namespace K4os.Compression.LZ4
{
	public unsafe class LZ4Encoder: IDisposable
	{
		private readonly LZ4_xx.LZ4_stream_t* _state;
		private readonly byte* _inputBuffer;
		private readonly byte* _outputBuffer;
		private int _disposed;
		private readonly int _inputLength;
		private readonly int _outputLength;
		private int _inputIndex;
		private int _outputIndex;

		public LZ4Encoder(int inputLength)
		{
			_state = (LZ4_xx.LZ4_stream_t*) Mem.Alloc(sizeof(LZ4_xx.LZ4_stream_t));
			_inputIndex = 0;
			_outputIndex = 0;
			_inputLength = Math.Max(inputLength, 1024);
			_outputLength = 0x10000 + LZ4_xx.LZ4_compressBound(_inputLength);
			_inputBuffer = (byte*) Mem.Alloc(_inputLength);
			_outputBuffer = (byte*) Mem.Alloc(_outputLength);
		}

		public IEnumerable<byte[]> Encode(byte[] source, int sourceIndex, int sourceLength)
		{
			if (Interlocked.CompareExchange(ref _disposed, 0, 0) != 0)
				throw new InvalidOperationException("Cannot use disposed encoder");

			if (sourceLength <= 0)
				yield break;

			while (sourceLength > 0)
			{
				var chunk = Math.Min(_inputLength - _inputIndex, sourceLength);
				if (chunk != 0)
				{
					Append(source, sourceIndex, chunk);
					_inputIndex += chunk;
					sourceIndex += chunk;
					sourceLength -= chunk;
				}

				if (_inputIndex >= _inputLength)
				{
					yield return Encode();
					_inputIndex = 0;
				}
			}
		}

		private byte[] Encode()
		{
			LZ4_64.LZ4_compress_fast_extState()
		}

		private void Append(byte[] source, int sourceIndex, int sourceLength)
		{
			fixed (byte* sourceP = source)
				Mem.Copy(_inputBuffer, sourceP + sourceIndex, sourceLength);
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
