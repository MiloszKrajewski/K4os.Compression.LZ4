using K4os.Compression.LZ4.Engine;
using K4os.Compression.LZ4.Internal;

namespace K4os.Compression.LZ4.Encoders
{
	// fast encoder context
	using LZ4Context = LZ4_xx.LZ4_stream_t;

	/// <summary>
	/// LZ4 encoder using dependent blocks with fast compression.
	/// </summary>
	public unsafe class LZ4FastChainEncoder: LZ4EncoderBase
	{
		private readonly LZ4Context* _context;

		/// <summary>Creates new instance of <see cref="LZ4FastChainEncoder"/></summary>
		/// <param name="blockSize">Block size.</param>
		/// <param name="extraBlocks">Number of extra blocks.</param>
		public LZ4FastChainEncoder(int blockSize, int extraBlocks = 0):
			base(true, blockSize, extraBlocks)
		{
			_context = (LZ4Context*) Mem.AllocZero(sizeof(LZ4Context));
		}

		/// <inheritdoc />
		protected override void ReleaseUnmanaged()
		{
			base.ReleaseUnmanaged();
			Mem.Free(_context);
		}

		/// <inheritdoc />
		protected override int EncodeBlock(
			byte* source, int sourceLength, byte* target, int targetLength) =>
			LZ4_64.LZ4_compress_fast_continue(
				_context, source, target, sourceLength, targetLength, 1);

		/// <inheritdoc />
		protected override int CopyDict(byte* target, int length) =>
			LZ4_xx.LZ4_saveDict(_context, target, length);
	}
}
