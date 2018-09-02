namespace K4os.Compression.LZ4.Internal
{
	public class Bytes
	{
		private static ulong ZigZagToVarInt(long value) =>
			(ulong) (value << 1) ^ (ulong) (value >> 63);

		private static long VarIntToZigZag(ulong value) =>
			(long) (value >> 1) ^ -(long) (value & 1);

		public static void Write8(byte[] buffer, ref int offset, byte value) =>
			buffer[offset++] = value;

		public static byte Read8(byte[] buffer, ref int offset) =>
			buffer[offset++];

		public static void WriteVarInt(byte[] buffer, ref int offset, ulong value)
		{
			while (value > 0x7F)
			{
				buffer[offset++] = (byte) (value | 0x80);
				value >>= 7;
			}

			buffer[offset++] = (byte) value;
		}

		public static ulong ReadVarInt(byte[] buffer, ref int offset)
		{
			var result = 0uL;
			var shl = 0;
			while (true)
			{
				var nibble = buffer[offset++];
				result |= (ulong) (nibble & 0x7F) << shl;
				if (nibble <= 0x7F) break;

				shl += 7;
			}

			return result;
		}
	}
}
