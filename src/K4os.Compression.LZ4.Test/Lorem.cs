using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace K4os.Compression.LZ4.Test
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
	}
}
