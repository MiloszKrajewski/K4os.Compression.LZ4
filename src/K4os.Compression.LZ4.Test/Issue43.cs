using System;
using System.IO;
using System.Linq;
using TestHelpers;
using Xunit;

namespace K4os.Compression.LZ4.Test
{
	public class Issue43
	{
		[Theory]
		[InlineData(".corpus/mozilla")]
		[InlineData(".corpus/webster")]
		public void LoadWhileFileAndCompress(string filename)
		{
			var original = File.ReadAllBytes(Tools.FindFile(filename));

			var compressed = Compress(original);
			var decompressed = Decompress(compressed, original.Length);

			Tools.SameBytes(original, decompressed);
		}
		
		[Theory]
		[InlineData(".corpus/mozilla")]
		[InlineData(".corpus/webster")]
		public void PickleAndUnpickle(string filename)
		{
			var original = File.ReadAllBytes(Tools.FindFile(filename));

			var compressed = LZ4Pickler.Pickle(original);
			var decompressed = LZ4Pickler.Unpickle(compressed);

			Tools.SameBytes(original, decompressed);
		}

		static byte[] Compress(byte[] source)
		{
			var target = new byte[LZ4Codec.MaximumOutputSize(source.Length)];
			var encoded = LZ4Codec.Encode(
				source, 0, source.Length,
				target, 0, target.Length);

			return target.AsSpan(0, encoded).ToArray();
		}

		static byte[] Decompress(byte[] source, int originalLength)
		{
			var target = new byte[originalLength];
			var decoded = LZ4Codec.Decode(
				source, 0, source.Length,
				target, 0, target.Length);

			return target.AsSpan(0, decoded).ToArray();
		}
	}
}
