namespace K4os.Compression.LZ4
{
	public unsafe class Mem
	{
		internal static void Copy(byte* target, byte* source, int length)
		{
			while (length > sizeof(ulong))
			{
				*(ulong*) target = *(ulong*) source;
				target += sizeof(ulong);
				source += sizeof(ulong);
				length -= sizeof(ulong);
			}

			while (length > sizeof(uint))
			{
				*(uint*) target = *(uint*) source;
				target += sizeof(uint);
				source += sizeof(uint);
				length -= sizeof(uint);
			}

			while (length > 0)
			{
				*target = *source;
				target += sizeof(byte);
				source += sizeof(byte);
				length -= sizeof(byte);
			}
		}

		public static void Zero(byte* target, int length)
		{
			while (length > sizeof(ulong))
			{
				*(ulong*) target = 0;
				target += sizeof(ulong);
				length -= sizeof(ulong);
			}

			while (length > sizeof(uint))
			{
				*(uint*) target = 0;
				target += sizeof(uint);
				length -= sizeof(uint);
			}

			while (length > 0)
			{
				*target = 0;
				target++;
				length--;
			}
		}
	}
}
