using System;
using K4os.Compression.LZ4.Internal;

namespace K4os.Compression.LZ4.Encoders
{
	public unsafe class LZ4FastEncoder: LZ4AbstractEncoder
	{
		private readonly LZ4_xx.LZ4_stream_t* _state;

		public LZ4FastEncoder(int blockSize): base(blockSize)
		{
			_state = (LZ4_xx.LZ4_stream_t*) Mem.AllocZero(sizeof(LZ4_xx.LZ4_stream_t));
		}

		protected override int EncodeBlock(byte* source, int sourceLength, byte* target, int targetLength)
		{
			var encoded = LZ4_64.LZ4_compress_fast_continue(_state, source, target, sourceLength, targetLength, 1);
			if (encoded == 0)
				throw new InvalidOperationException("Output buffer too small");
			if (encoded < 0)
				throw new InvalidOperationException("Unspecified error when compressing");
			return encoded;
		}

		protected override void DisposeUnmanaged()
		{
			base.DisposeUnmanaged();
			Mem.Free(_state);
		}
	}
}
