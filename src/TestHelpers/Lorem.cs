﻿using System;
using System.Text;
using K4os.Compression.LZ4.Internal;

namespace TestHelpers
{
	public static class Lorem
	{
		public const string Text =
			"Lorem ipsum dolor sit amet, consectetur adipiscing elit, " +
			"sed do eiusmod tempor incididunt ut labore et dolore magna aliqua. " +
			"Ut enim ad minim veniam, quis nostrud exercitation ullamco laboris " +
			"nisi ut aliquip ex ea commodo consequat. Duis aute irure dolor in reprehenderit " +
			"in voluptate velit esse cillum dolore eu fugiat nulla pariatur. " +
			"Excepteur sint occaecat cupidatat non proident, sunt in culpa qui officia " +
			"deserunt mollit anim id est laborum. ";

		public static readonly byte[] Bytes = Encoding.UTF8.GetBytes(Text);

		public static byte[] Create(int length)
		{
			var result = new byte[length];
			Fill(result, 0, length);
			return result;
		}

		public static void Fill(byte[] output, int outputIndex, int outputLength)
		{
			while (outputLength > 0)
			{
				var chunk = Math.Min(outputLength, Bytes.Length);
				Array.Copy(Bytes, 0, output, outputIndex, chunk);
				outputLength -= chunk;
				outputIndex += chunk;
			}
		}

		public static unsafe void Fill(byte* output, int outputLength)
		{
			fixed (byte* bytesP = Bytes)
			{
				while (outputLength > 0)
				{
					var chunk = Math.Min(outputLength, Bytes.Length);
					Mem.Copy(output, bytesP, chunk);
					outputLength -= chunk;
					output += chunk;
				}
			}
		}
	}
}
