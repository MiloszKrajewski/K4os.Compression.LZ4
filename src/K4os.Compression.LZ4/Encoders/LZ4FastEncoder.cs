using K4os.Compression.LZ4.Internal;

namespace K4os.Compression.LZ4.Encoders
{
	public unsafe class LZ4FastEncoder: LZ4AbstractEncoder
	{
		private readonly LZ4_xx.LZ4_stream_t* _state;

		public LZ4FastEncoder(int blockSize): base(blockSize)
		{
			_state = (LZ4_xx.LZ4_stream_t*) Mem.Alloc(sizeof(LZ4_xx.LZ4_stream_t));
			Mem.Zero((byte*) _state, sizeof(LZ4_xx.LZ4_stream_t));
		}

		protected override int EncodeBlock(byte* source, int sourceLength, byte* target, int targetLength)
		{
			Mem.Copy(target, source, sourceLength);
			return targetLength;
		}

		protected override void DisposeUnmanaged()
		{
			base.DisposeUnmanaged();
			Mem.Free(_state);
		}
	}
}
