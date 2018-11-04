using System;
using K4os.Compression.LZ4.Engine;
using K4os.Compression.LZ4.Internal;

namespace K4os.Compression.LZ4.Encoders
{
	// fast decoder context
	using LZ4Context = LZ4_xx.LZ4_streamDecode_t;

	/// <summary>LZ4 decoder handling dependent blocks.</summary>
	public unsafe class LZ4ChainDecoder: UnmanagedResources, ILZ4Decoder
	{
		private readonly LZ4Context* _context;
		private readonly int _blockSize;
		private readonly byte* _outputBuffer;
		private readonly int _outputLength;
		private int _outputIndex;

		/// <summary>Creates new instance of <see cref="LZ4ChainDecoder"/>.</summary>
		/// <param name="blockSize">Block size.</param>
		/// <param name="extraBlocks">Number of extra blocks.</param>
		public LZ4ChainDecoder(int blockSize, int extraBlocks)
		{
			blockSize = Mem.RoundUp(Math.Max(blockSize, Mem.K1), Mem.K1);
			extraBlocks = Math.Max(extraBlocks, 0);

			_blockSize = blockSize;
			_outputLength = Mem.K64 + (1 + extraBlocks) * _blockSize + 8;
			_outputIndex = 0;
			_outputBuffer = (byte*) Mem.Alloc(_outputLength + 8);
			_context = (LZ4Context*) Mem.AllocZero(sizeof(LZ4Context));
		}

		/// <inheritdoc />
		public int BlockSize => _blockSize;

		/// <inheritdoc />
		public int BytesReady => _outputIndex;

		/// <inheritdoc />
		public int Decode(byte* source, int length, int blockSize)
		{
			if (blockSize <= 0)
				blockSize = _blockSize;

			Prepare(blockSize);

			var decoded = DecodeBlock(source, length, _outputBuffer + _outputIndex, blockSize);

			if (decoded < 0)
				throw new InvalidOperationException();

			_outputIndex += decoded;

			return decoded;
		}

		/// <inheritdoc />
		public int Inject(byte* source, int length)
		{
			if (length <= 0)
				return 0;

			if (length > Math.Max(_blockSize, Mem.K64))
				throw new InvalidOperationException();

			if (_outputIndex + length < _outputLength)
			{
				Mem.Move(_outputBuffer + _outputIndex, source, length);
				_outputIndex = ApplyDict(_outputIndex + length);
			} 
			else if (length >= Mem.K64)
			{
				Mem.Move(_outputBuffer, source, length);
				_outputIndex = ApplyDict(length);
			}
			else
			{
				var tailSize = Math.Min(Mem.K64 - length, _outputIndex);
				Mem.Move(_outputBuffer, _outputBuffer + _outputIndex - tailSize, tailSize);
				Mem.Move(_outputBuffer + tailSize, source, length);
				_outputIndex = ApplyDict(tailSize + length);
			}

			return length;
		}

		/// <inheritdoc />
		public void Drain(byte* target, int offset, int length)
		{
			offset = _outputIndex + offset; // NOTE: negative value
			if (offset < 0 || length < 0 || offset + length > _outputIndex)
				throw new InvalidOperationException();

			Mem.Move(target, _outputBuffer + offset, length);
		}

		private void Prepare(int blockSize)
		{
			if (_outputIndex + blockSize <= _outputLength)
				return;

			_outputIndex = CopyDict(_outputIndex);
		}

		private int CopyDict(int index)
		{
			var dictStart = Math.Max(index - Mem.K64, 0);
			var dictSize = index - dictStart;
			Mem.Move(_outputBuffer, _outputBuffer + dictStart, dictSize);
			LZ4_xx.LZ4_setStreamDecode(_context, _outputBuffer, dictSize);
			return dictSize;
		}

		private int ApplyDict(int index)
		{ 
			var dictStart = Math.Max(index - Mem.K64, 0);
			var dictSize = index - dictStart;
			LZ4_xx.LZ4_setStreamDecode(_context, _outputBuffer + dictStart, dictSize);
			return index;
		}

		private int DecodeBlock(byte* source, int sourceLength, byte* target, int targetLength) =>
			LZ4_xx.LZ4_decompress_safe_continue(_context, source, target, sourceLength, targetLength);

		/// <inheritdoc />
		protected override void ReleaseUnmanaged()
		{
			base.ReleaseUnmanaged();
			Mem.Free(_context);
			Mem.Free(_outputBuffer);
		}
	}
}
