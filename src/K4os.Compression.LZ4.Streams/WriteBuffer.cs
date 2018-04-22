namespace K4os.Compression.LZ4.Streams
{
	public unsafe class ByteBuffer
	{
		public void WriteByte(byte* buffer, ref int offset, byte value) =>
			buffer[offset++] = value;

		public void Write16(byte* buffer, ref int offset, ushort value)
		{
			*((ushort*) &buffer[offset]) = value;
			offset += sizeof(ushort);
		}

		public void Write32(byte* buffer, ref int offset, uint value)
		{
			*((uint*) &buffer[offset]) = value;
			offset += sizeof(uint);
		}

		public void Write64(byte* buffer, ref int offset, ulong value)
		{
			*((ulong*) &buffer[offset]) = value;
			offset += sizeof(ulong);
		}
	}
}
