using System;
using K4os.Compression.LZ4.Internal;

namespace K4os.Compression.LZ4.Streams
{
	public partial class LZ4DecoderStream
	{
		// ReSharper disable once InconsistentNaming
		private const int _length16 = 16; // we intend to use only 16 bytes
		private readonly byte[] _buffer16 = new byte[_length16 + 8];
		private int _index16;

		private static int MaxBlockSize(int blockSizeCode) =>
			blockSizeCode switch {
				7 => Mem.M4, 6 => Mem.M1, 5 => Mem.K256, 4 => Mem.K64, _ => Mem.K64
			};
		
		private int PeekN(byte[] buffer, int offset, int count, bool optional = false)
		{
			var index = 0;
			while (count > 0)
			{
				var read = _inner.Read(buffer, index + offset, count);
				if (read == 0)
				{
					if (index == 0 && optional)
						return 0;

					throw EndOfStream();
				}

				index += read;
				count -= read;
			}

			return index;
		}

		private bool PeekN(int count, bool optional = false)
		{
			if (count == 0) return true;

			var read = PeekN(_buffer16, _index16, count, optional);
			_index16 += read;
			return read > 0;
		}

		private void FlushPeek() { _index16 = 0; }

		// ReSharper disable once UnusedMethodReturnValue.Local
		private ulong Peek64()
		{
			PeekN(sizeof(ulong));
			return BitConverter.ToUInt64(_buffer16, _index16 - sizeof(ulong));
		}

		private uint? TryPeek32()
		{
			if (!PeekN(sizeof(uint), true))
				return null;

			return BitConverter.ToUInt32(_buffer16, _index16 - sizeof(uint));
		}

		private uint Peek32()
		{
			PeekN(sizeof(uint));
			return BitConverter.ToUInt32(_buffer16, _index16 - sizeof(uint));
		}

		private ushort Peek16()
		{
			PeekN(sizeof(ushort));
			return BitConverter.ToUInt16(_buffer16, _index16 - sizeof(ushort));
		}

		private byte Peek8()
		{
			PeekN(sizeof(byte));
			return _buffer16[_index16 - 1];
		}

	}
}
