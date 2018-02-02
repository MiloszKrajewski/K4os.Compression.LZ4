using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using ICSharpCode.SharpZipLib.BZip2;
using LZ4;
using Xunit;

namespace K4os.Compression.LZ4.Test
{
	public class UnitTest1
	{
		const string Lorem =
			"Lorem ipsum dolor sit amet, consectetur adipiscing elit, " +
			"sed do eiusmod tempor incididunt ut labore et dolore magna aliqua. " +
			"Ut enim ad minim veniam, quis nostrud exercitation ullamco laboris " +
			"nisi ut aliquip ex ea commodo consequat. Duis aute irure dolor in " +
			"reprehenderit in voluptate velit esse cillum dolore eu fugiat nulla pariatur. " +
			"Excepteur sint occaecat cupidatat non proident, " +
			"sunt in culpa qui officia deserunt mollit anim id est laborum.";

		public async Task<string> Download(string url)
		{
			var uri = new Uri(url);
			var fileName = Path.GetFileName(uri.LocalPath);
			var dataFile = Path.ChangeExtension(fileName, ".dat");
			if (File.Exists(dataFile))
				return dataFile;

			var client = new WebClient();
			await client.DownloadFileTaskAsync(uri, fileName);

			using (var bz = File.OpenRead(fileName))
			using (var dt = File.OpenWrite(dataFile))
				BZip2.Decompress(bz, dt, true);

			return dataFile;
		}

		public byte[] Collect(IEnumerable<byte[]> chunks)
		{
			var arrays = chunks.ToArray();
			var length = arrays.Select(x => x.Length).Sum();
			var result = new byte[length];
			var index = 0;
			foreach (var c in arrays)
			{
				Array.Copy(c, 0, result, index, c.Length);
				index += c.Length;
			}

			return result;
		}

		public void Roundtrip(byte[] source)
		{
			var compressedOld = LZ4Codec.Encode(source, 0, source.Length);
			var compressedNew = LZ4Interface.Encode(source);

			Assert.Equal(
				source,
				LZ4Codec.Decode(compressedNew, 0, compressedNew.Length, source.Length));

			Assert.Equal(
				source,
				LZ4Interface.Decode(compressedNew, source.Length));

			Assert.Equal(
				source,
				LZ4Interface.Decode(compressedOld, source.Length));
		}

		[Theory]
		[InlineData(160)]
		[InlineData(0)]
		[InlineData(255)]
		[InlineData(65)]
		public void SingleByteRountrip(byte value)
		{
			Roundtrip(new[] { value });
		}

		[Theory]
		[InlineData(160, 33)]
		[InlineData(0, 13)]
		[InlineData(0, 15)]
		[InlineData(0, 17)]
		[InlineData(255, 1000)]
		[InlineData(65, 67)]
		public void RepeatedByteRountrip(byte value, int length)
		{
			var bytes = new byte[length];
			for (var i = 0; i < length; i++) bytes[i] = value;

			Roundtrip(bytes);
		}

		[Theory]
		[InlineData(1)]
		[InlineData(1000)]
		[InlineData(0x7FFF)]
		[InlineData(0xFFFF)]
		[InlineData(0x123456)]
		public void RepeatLoremIpsum(int length)
		{
			var loremBytes = Encoding.UTF8.GetBytes(Lorem);
			var buffer = Collect(Enumerable.Repeat(loremBytes, length / Lorem.Length + 1));

			Roundtrip(buffer);
		}

		[Theory]
		[InlineData(0, 1000)]
		[InlineData(1, 0x7FFF)]
		[InlineData(2, 0xFFFF)]
		[InlineData(3, 0xFFFF)]
		[InlineData(4, 0x123456)]
		public void UncompressibleData(int seed, int length)
		{
			var buffer = new byte[length];
			new Random(seed).NextBytes(buffer);

			Roundtrip(buffer);
		}

		[Theory]
		[InlineData("http://sun.aei.polsl.pl/~sdeor/corpus/dickens.bz2")]
		public async Task SilesiaCorpus(string url)
		{
			var fileName = await Download(url);
			var bytes = File.ReadAllBytes(fileName);
			Roundtrip(bytes);
		}

		//[Fact]
		//public void Test1()
		//{
		//	// var template = "\u0000\u0001\u0002\u0003\u0004\u0005\u0006\u0007\u0008\u0009";
		//	var template = Lorem + Lorem + Lorem + Lorem + Lorem;
		//	var source = Encoding.UTF8.GetBytes(Enumerable.Repeat(template, 3).Aggregate((a, b) => a + b));
		//	var target1 = LZ4Codec.Encode(source, 0, source.Length);
		//	var target2 = new byte[LZ4Interface.MaximumOutputSize(source.Length)];
		//	LZ4Interface.Encode(source, target2);
		//	var diff = Compare(target1, target2, target1.Length);
		//	Array.Clear(target2, 0, target2.Length);
		//	var compressedLength = LZ4Interface.Encode(source, target2);
		//	var result = new byte[source.Length];
		//	var decompressedLength = LZ4Interface.Decode(target2, result, compressedLength);

		//	diff = Compare(source, result);
		//	if (diff >= 0)
		//		throw new Exception($"Difference found @ {diff}/{source.Length}");
		//}

		private static int Compare(byte[] source, byte[] target, int length = -1)
		{
			if (length < 0)
			{
				length = source.Length;
				if (length != target.Length)
					return 0;
			}

			for (var i = 0; i < length; i++)
			{
				if (source[i] != target[i])
					return i;
			}

			return -1;
		}
	}
}
