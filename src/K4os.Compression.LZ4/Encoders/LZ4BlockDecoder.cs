using System;
using K4os.Compression.LZ4.Internal;

namespace K4os.Compression.LZ4.Encoders
{
	public unsafe class LZ4BlockDecoder: UnmanagedResources, ILZ4Decoder
	{
		private readonly int _blockSize;
		private readonly int _outputLength;
		private int _outputIndex;
		private readonly byte* _outputBuffer;

		public int BlockSize => _blockSize;

		public int BytesReady => _outputIndex;

		public LZ4BlockDecoder(int blockSize)
		{
			blockSize = Mem.RoundUp(Math.Max(blockSize, Mem.K1), Mem.K1);
			_blockSize = blockSize;
			_outputLength = _blockSize + 8;
			_outputIndex = 0;
			_outputBuffer = (byte*) Mem.Alloc(_outputLength + 8);
		}

		public int Decode(byte* source, int length, int blockSize = 0)
		{
			ThrowIfDisposed();
			
			if (blockSize <= 0)
				blockSize = _blockSize;

			if (blockSize > _blockSize)
				throw new InvalidOperationException();

			var decoded = LZ4Codec.Decode(source, length, _outputBuffer, _outputLength);
			if (decoded < 0)
				throw new InvalidOperationException();

			_outputIndex = decoded;
			return _outputIndex;
		}

		public int Inject(byte* source, int length)
		{
			ThrowIfDisposed();
			
			if (length <= 0)
				return _outputIndex = 0;

			if (length > _outputLength)
				throw new InvalidOperationException();

			Mem.Move(_outputBuffer, source, length);
			_outputIndex = length;
			return length;
		}

		public void Drain(byte* target, int offset, int length)
		{
			ThrowIfDisposed();

			offset = _outputIndex + offset; // NOTE: negative value
			if (offset < 0 || length < 0 || offset + length > _outputIndex)
				throw new InvalidOperationException();

			Mem.Move(target, _outputBuffer + offset, length);
		}

		protected override void ReleaseUnmanaged()
		{
			base.ReleaseUnmanaged();
			Mem.Free(_outputBuffer);
		}
	}
}
