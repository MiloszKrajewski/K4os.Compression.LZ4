namespace K4os.Compression.LZ4.Encoders
{
	public unsafe class LZ4NoopEncoder: LZ4AbstractEncoder
	{
		public LZ4NoopEncoder(int blockSize): base(blockSize) { }

		protected override int EncodeBlock(byte* source, int sourceLength, byte* target, int targetLength)
		{
			Mem.Copy(target, source, sourceLength);
			return targetLength;
		}
	}
}
