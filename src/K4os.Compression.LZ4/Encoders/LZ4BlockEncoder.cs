using System;
using K4os.Compression.LZ4.Internal;

namespace K4os.Compression.LZ4.Encoders
{
	public unsafe class LZ4BlockEncoder: LZ4EncoderBase
	{
		private readonly LZ4Level _level;

		public LZ4BlockEncoder(LZ4Level level, int blockSize): base(0, blockSize, 0)
		{
			_level = level;
		}

		protected override int EncodeBlock(
			byte* source, int sourceLength, byte* target, int targetLength) =>
			LZ4Codec.Encode(source, sourceLength, target, targetLength, _level);

		protected override int CopyDict(byte* buffer, int dictionaryLength) => 0;
	}
}
