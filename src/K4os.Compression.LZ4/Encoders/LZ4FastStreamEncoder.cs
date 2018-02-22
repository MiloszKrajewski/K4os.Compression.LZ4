using K4os.Compression.LZ4.Internal;

namespace K4os.Compression.LZ4.Encoders
{
	public unsafe class LZ4FastStreamEncoder: LZ4StreamEncoder
	{
		private readonly LZ4_xx.LZ4_stream_t* _context;

		public LZ4FastStreamEncoder(int blockSize, int extraBlocks = 0): base(blockSize, extraBlocks)
		{
			_context = (LZ4_xx.LZ4_stream_t*) Mem.AllocZero(sizeof(LZ4_xx.LZ4_stream_t));
		}

		protected override void ReleaseUnmanaged()
		{
			base.ReleaseUnmanaged();
			Mem.Free(_context);
		}

		protected override int EncodeBlock(
			byte* source, int sourceLength, byte* target, int targetLength) =>
			LZ4_64.LZ4_compress_fast_continue(_context, source, target, sourceLength, targetLength, 1);

		protected override void SetupPrefix(byte* dictionary, int dictionaryLength) =>
			LZ4_64.LZ4_loadDict(_context, dictionary, dictionaryLength);
	}
}
