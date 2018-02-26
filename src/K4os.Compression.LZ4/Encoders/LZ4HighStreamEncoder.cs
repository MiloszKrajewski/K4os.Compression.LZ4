using K4os.Compression.LZ4.Internal;

namespace K4os.Compression.LZ4.Encoders
{
	using LZ4Context = LZ4_64_HC.LZ4HC_CCtx_t;

	public unsafe class LZ4HighStreamEncoder: LZ4StreamEncoder
	{
		private readonly LZ4Context* _context;

		public LZ4HighStreamEncoder(int blockSize, int extraBlocks = 0): base(blockSize, extraBlocks)
		{
			_context = (LZ4Context*) Mem.AllocZero(sizeof(LZ4Context));
			LZ4_64_HC.cont
		}

		protected override void ReleaseUnmanaged()
		{
			base.ReleaseUnmanaged();
			Mem.Free(_context);
		}

		protected override int EncodeBlock(
			byte* source, int sourceLength, byte* target, int targetLength) =>
			LZ4_64_HC.LZ4_compress_HC_continue(_context, source, target, sourceLength, targetLength);

		protected override int CopyDict(byte* buffer, int length) =>
			LZ4_64_HC.LZ4_saveDictHC(_context, buffer, length);
	}
}
