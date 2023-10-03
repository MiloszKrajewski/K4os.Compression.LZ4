using System;
using K4os.Compression.LZ4.Internal;

namespace K4os.Compression.LZ4.Streams.Tests.Internal
{
	public class Settings
	{
		public static LZ4Settings ParseSettings(string options)
		{
			var result = new LZ4Settings {
				Chaining = false,
				ContentChecksum = true,
			};

			foreach (var option in options.Split(' '))
			{
				switch (option)
				{
					case "-1":
						result.Level = LZ4Level.L00_FAST;
						break;
					case "-9":
						result.Level = LZ4Level.L09_HC;
						break;
					case "-11":
						result.Level = LZ4Level.L11_OPT;
						break;
					case "-12":
						result.Level = LZ4Level.L12_MAX;
						break;
					case "-BD":
						result.Chaining = true;
						break;
					case "-BX":
						result.BlockChecksum = true;
						break;
					case "--no-frame-crc":
						result.ContentChecksum = false;
						break;
					case "-B4":
						result.BlockSize = Mem.K64;
						break;
					case "-B5":
						result.BlockSize = Mem.K256;
						break;
					case "-B6":
						result.BlockSize = Mem.M1;
						break;
					case "-B7":
						result.BlockSize = Mem.M4;
						break;
					default:
						throw new NotImplementedException($"Option '{option}' not recognized");
				}
			}

			return result;
		}

	}
}
