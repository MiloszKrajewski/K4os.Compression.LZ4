using K4os.Compression.LZ4.Engine;
using K4os.Compression.LZ4.Internal;

namespace K4os.Compression.LZ4.Encoders
{
	// high encoder context
	using LZ4Context = LZ4_64_HC.LZ4HC_CCtx_t;

	public unsafe class LZ4HighChainEncoder: LZ4EncoderBase
	{
		private readonly LZ4Context* _context;

		public LZ4HighChainEncoder(LZ4Level level, int blockSize, int extraBlocks = 0):
			base(Mem.K64, blockSize, extraBlocks)
		{
			if (level < LZ4Level.L03_HC) level = LZ4Level.L03_HC;
			if (level > LZ4Level.L12_MAX) level = LZ4Level.L12_MAX;
			_context = (LZ4Context*) Mem.AllocZero(sizeof(LZ4Context));
			LZ4_64_HC.LZ4_resetStreamHC(_context, (int) level);
		}

		protected override void ReleaseUnmanaged()
		{
			base.ReleaseUnmanaged();
			Mem.Free(_context);
		}

		protected override int EncodeBlock(
			byte* source, int sourceLength, byte* target, int targetLength) =>
			LZ4_64_HC.LZ4_compress_HC_continue(
				_context, source, target, sourceLength, targetLength);

		protected override int CopyDict(byte* buffer, int length) =>
			LZ4_64_HC.LZ4_saveDictHC(_context, buffer, length);
	}
}
