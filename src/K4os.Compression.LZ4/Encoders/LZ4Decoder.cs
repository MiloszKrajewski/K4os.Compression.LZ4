using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using K4os.Compression.LZ4.Internal;
using LZ4DecoderContext = K4os.Compression.LZ4.Internal.LZ4_xx.LZ4_streamDecode_t;

namespace K4os.Compression.LZ4.Encoders
{
	public unsafe class LZ4Decoder: IDisposable
	{
		private LZ4DecoderContext* _state;
		private int _disposed;
		private byte* _outputBuffer;
		private int _outputLength;
		private int _outputIndex;
		private int _blockSize;

		public LZ4Decoder(int blockSize)
		{
			_state = (LZ4DecoderContext*) Mem.AllocZero(sizeof(LZ4DecoderContext));
			_blockSize = Math.Max(blockSize, 1024);
			_outputLength = OptimalOutputBufferSize(_blockSize);
			_outputBuffer = (byte*) Mem.Alloc(_outputLength);
		}

		private static int OptimalOutputBufferSize(int blockSize) =>
			Roundup(Math.Max(blockSize, 0x10000) + 2 * blockSize, blockSize);

		private static int Roundup(int value, int step) => (value + step - 1) / step * step;

		protected void DisposeUnmanaged()
		{
			Mem.Free(_state);
			Mem.Free(_outputBuffer);
		}

		protected virtual void DisposeManaged() { }

		protected virtual void Dispose(bool disposing)
		{
			if (Interlocked.CompareExchange(ref _disposed, 1, 0) != 0)
				return;

			DisposeUnmanaged();
			if (disposing)
				DisposeManaged();
		}

		public void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		~LZ4Decoder()
		{
			Dispose(false);
		}

		// xxxx12345eee
		// xxxx123ee
	}
}
