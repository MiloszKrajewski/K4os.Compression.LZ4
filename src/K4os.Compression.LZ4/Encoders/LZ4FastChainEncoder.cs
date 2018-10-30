using K4os.Compression.LZ4.Engine;
using K4os.Compression.LZ4.Internal;

namespace K4os.Compression.LZ4.Encoders
{
	// fast encoder context
	using LZ4Context = LZ4_xx.LZ4_stream_t;

	public unsafe class LZ4FastChainEncoder: LZ4EncoderBase
	{
		private readonly LZ4Context* _context;

		public LZ4FastChainEncoder(int blockSize, int extraBlocks = 0): 
			base(Mem.K64, blockSize, extraBlocks)
		{
			_context = (LZ4Context*) Mem.AllocZero(sizeof(LZ4Context));
		}

		protected override void ReleaseUnmanaged()
		{
			base.ReleaseUnmanaged();
			Mem.Free(_context);
		}

		protected override int EncodeBlock(
			byte* source, int sourceLength, byte* target, int targetLength) =>
			LZ4_64.LZ4_compress_fast_continue(
				_context, source, target, sourceLength, targetLength, 1);

		protected override int CopyDict(byte* buffer, int length) =>
			LZ4_xx.LZ4_saveDict(_context, buffer, length);
	}
}
