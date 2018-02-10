using System;
using Xunit;

namespace K4os.Compression.LZ4.Test
{
	public class SilesiaCorpusBlockTests
	{
		[Theory]
		// 64
		[InlineData(".corpus/dickens", 0, 10192446, 6428742, 0x17278caf, "+qMqKlRoZSBQcm9qZWN0IEd1dGVuYmVyZyBFdGV4dCBvZiBBIENoaWxkJ3MgSGlzdG9yeSBvZiBFbmds")]
		[InlineData(".corpus/mozilla", 0, 51220480, 26435667, 0x929ed42e, "n21vemlsbGEvAAEASPQGIDQwNzU1IAAgIDI2MDAgACAgICAgCAABAgD/CDAgIDc0NzU3NDI3NjEgIDEw")]
		[InlineData(".corpus/mr", 0, 9970564, 5440937, 0x5d218bd6, "+EgIAAUACgAAAElTT19JUiAxMDAIAAgAFgAAAE9SSUdJTkFMXFBSSU1BUllcT1RIRVIIABYAGgAAADEu")]
		[InlineData(".corpus/nci", 0, 33553445, 5533040, 0x20c1d85f, "9hwxNTU1NDIKUk90Y2xzZXJ2ZTExMTUwMDExMjEyRCAwICAgMC4wMDAwMCAgDAD/BDEwNDk1MjEKIAog")]
		[InlineData(".corpus/ooffice", 0, 6152192, 4338918, 0xf6e8e90, "8gNNWpAAAwAAAAQAAAD//wAAuAABABJABwAPAgAK8y7wAAAADh+6DgC0Cc0huAFMzSFUaGlzIHByb2dy")]
		[InlineData(".corpus/osdb", 0, 10085684, 5256666, 0x6f12d3ea, "8w4DAE8BYAIThAEAAHUlBa4AAAC5za/NhecSTgw2MQIA8T0uMDAJNy8xNy8xOTQ0RmhYVHViOlpRTjVt")]
		[InlineData(".corpus/reymont", 0, 6627202, 3181387, 0x41648906, "8hElUERGLTEuMwozIDAgb2JqIDw8Ci9MZW5ndGggMTUzIAEA8SgKPj4Kc3RyZWFtCjEgMCAwIDEgMjQ0")]
		[InlineData(".corpus/samba", 0, 21606400, 7716839, 0x93086c52, "73NhbWJhLTIuMi4zYS8AAQBD8QgwMDQwNzU1ADAwMDE3NjEAMDAwMDE1MggAAwIA/wcAMDc0MjcxMDQw")]
		[InlineData(".corpus/sao", 0, 7251944, 6790273, 0x81d09df4, "sAAAAAABAAAAtfMDCwABDADwSQEAAAAcAAAA1Ke7C7dKOD9rphXawBf3P0Ew0AKZBiK1qpQmMrdL+Jif")]
		[InlineData(".corpus/webster", 0, 41458703, 20139988, 0x52b7fc61, "+sQNClRoZSBQcm9qZWN0IEd1dGVuYmVyZyBFdGV4dCBvZiBUaGUgMTkxMyBXZWJzdGVyIFVuYWJyaWRn")]
		[InlineData(".corpus/xml", 0, 5345280, 1227495, 0x3ae4f54c, "n2VsdHMueG1sAAEASOkxMDA3NzUgACAgIDc2NAgA/w8gIDMzNDc1NyAgNzE3NDM2NjM3MCAgMTIyMDEA")]
		[InlineData(".corpus/x-ray", 0, 8474240, 8390195, 0xcd8a167b, "/w/QAQAQB2wItgAQAAEBEQ6zRlNfQS4zMTk3LmltZwABAA9vQkxLTTE4KAAPBAIAUzgwMDEwMQDzDjgw")]
		// 32
		//[InlineData("D:/Projects/K4os.Compression.LZ4/.corpus/dickens", 0, 10192446, 6432049, 0x6144792e, "8CMqKlRoZSBQcm9qZWN0IEd1dGVuYmVyZyBFdGV4dCBvZiBBIENoaWxkJ3MgSGlzdG9yeRUA8CJFbmds")]
		//[InlineData("D:/Projects/K4os.Compression.LZ4/.corpus/mozilla", 0, 51220480, 26375278, 0xaaf353d0, "n21vemlsbGEvAAEASOAgNDA3NTUgACAgMjYwMAgANCAgIAgAAQIA/wgwICA3NDc1NzQyNzYxICAxMDc2")]
		//[InlineData("D:/Projects/K4os.Compression.LZ4/.corpus/mr", 0, 9970564, 5669219, 0xfcd03aa6, "8CEIAAUACgAAAElTT19JUiAxMDAIAAgAFgAAAE9SSUdJTkFMXFBSSU1BUllcT1RIRVIcAPAKGgAAADEu")]
		//[InlineData("D:/Projects/K4os.Compression.LZ4/.corpus/nci", 0, 33553445, 5877051, 0xd7feea9a, "8BYxNTU1NDIKUk90Y2xzZXJ2ZTExMTUwMDExMjEyRCAwICAgMC4wAQAQIAEAAwwA8AIxMDQ5NTIxCiAK")]
		//[InlineData("D:/Projects/K4os.Compression.LZ4/.corpus/ooffice", 0, 6152192, 4228589, 0x13f4365f, "8gNNWpAAAwAAAAQAAAD//wAAuAABABJABwAPAgAK8y7wAAAADh+6DgC0Cc0huAFMzSFUaGlzIHByb2dy")]
		//[InlineData("D:/Projects/K4os.Compression.LZ4/.corpus/osdb", 0, 10085684, 5223143, 0x4df76a, "8w4DAE8BYAIThAEAAHUlBa4AAAC5za/NhecSTgw2MQIA8T0uMDAJNy8xNy8xOTQ0RmhYVHViOlpRTjVt")]
		//[InlineData("D:/Projects/K4os.Compression.LZ4/.corpus/reymont", 0, 6627202, 3520792, 0xb81a006b, "8hElUERGLTEuMwozIDAgb2JqIDw8Ci9MZW5ndGggMTUzIAEA8SgKPj4Kc3RyZWFtCjEgMCAwIDEgMjQ0")]
		//[InlineData("D:/Projects/K4os.Compression.LZ4/.corpus/samba", 0, 21606400, 7897177, 0x8cd79ae9, "73NhbWJhLTIuMi4zYS8AAQBD8AAwMDQwNzU1ADAwMDE3NjEIAEEwMTUyCAADAgD/BwAwNzQyNzEwNDAy")]
		//[InlineData("D:/Projects/K4os.Compression.LZ4/.corpus/sao", 0, 7251944, 6595675, 0xecfd9131, "sAAAAAABAAAAtfMDCwABDAAAEADwRRwAAADUp7sLt0o4P2umFdrAF/c/QTDQApkGIrWqlCYyt0v4mJ/k")]
		//[InlineData("D:/Projects/K4os.Compression.LZ4/.corpus/webster", 0, 41458703, 20360352, 0x420439f9, "8BINClRoZSBQcm9qZWN0IEd1dGVuYmVyZyBFdGV4dCBvZiAfAPqfMTkxMyBXZWJzdGVyIFVuYWJyaWRn")]
		//[InlineData("D:/Projects/K4os.Compression.LZ4/.corpus/xml", 0, 5345280, 1288300, 0x55f0b8a6, "n2VsdHMueG1sAAEASOkxMDA3NzUgACAgIDc2NAgA/w8gIDMzNDc1NyAgNzE3NDM2NjM3MCAgMTIyMDEA")]
		//[InlineData("D:/Projects/K4os.Compression.LZ4/.corpus/x-ray", 0, 8474240, 8163278, 0xbdf802fe, "/w/QAQAQB2wItgAQAAEBEQ6zRlNfQS4zMTk3LmltZwABAA9vQkxLTTE4KAAPBAIAUzgwMDEwMQDzDjgw")]
		public void CompressedFilesBinaryIdentical(
			string filename, int index, int length, int expectedCompressedLength, uint expectedChecksum, string expectedBytes64)
		{
			var src = Tools.LoadChunk(Tools.FindFile(filename), index, length);

			var dst = LZ4Codec.Encode64(src, 0, src.Length);
			var cmp = LZ4Codec.Decode(dst, 0, dst.Length, src.Length);

			string AsHex(uint value) => $"0x{value:x8}";

			Tools.SameBytes(src, cmp);

			var expectedBytes = Convert.FromBase64String(expectedBytes64);
			Tools.SameBytes(expectedBytes, dst, expectedBytes.Length);
			Assert.Equal(expectedCompressedLength, dst.Length);
			Assert.Equal(AsHex(expectedChecksum), AsHex(Tools.Adler32(dst)));
		}


		[Theory]
		[InlineData(".corpus/dickens", 0, 10192446, 6428742, 0x17278caf, "+qMqKlRoZSBQcm9qZWN0IEd1dGVuYmVyZyBFdGV4dCBvZiBBIENoaWxkJ3MgSGlzdG9yeSBvZiBFbmds")]
		[InlineData(".corpus/mozilla", 0, 51220480, 26435667, 0x929ed42e, "n21vemlsbGEvAAEASPQGIDQwNzU1IAAgIDI2MDAgACAgICAgCAABAgD/CDAgIDc0NzU3NDI3NjEgIDEw")]
		[InlineData(".corpus/mr", 0, 9970564, 5440937, 0x5d218bd6, "+EgIAAUACgAAAElTT19JUiAxMDAIAAgAFgAAAE9SSUdJTkFMXFBSSU1BUllcT1RIRVIIABYAGgAAADEu")]
		[InlineData(".corpus/nci", 0, 33553445, 5533040, 0x20c1d85f, "9hwxNTU1NDIKUk90Y2xzZXJ2ZTExMTUwMDExMjEyRCAwICAgMC4wMDAwMCAgDAD/BDEwNDk1MjEKIAog")]
		[InlineData(".corpus/ooffice", 0, 6152192, 4338918, 0xf6e8e90, "8gNNWpAAAwAAAAQAAAD//wAAuAABABJABwAPAgAK8y7wAAAADh+6DgC0Cc0huAFMzSFUaGlzIHByb2dy")]
		[InlineData(".corpus/osdb", 0, 10085684, 5256666, 0x6f12d3ea, "8w4DAE8BYAIThAEAAHUlBa4AAAC5za/NhecSTgw2MQIA8T0uMDAJNy8xNy8xOTQ0RmhYVHViOlpRTjVt")]
		[InlineData(".corpus/reymont", 0, 6627202, 3181387, 0x41648906, "8hElUERGLTEuMwozIDAgb2JqIDw8Ci9MZW5ndGggMTUzIAEA8SgKPj4Kc3RyZWFtCjEgMCAwIDEgMjQ0")]
		[InlineData(".corpus/samba", 0, 21606400, 7716839, 0x93086c52, "73NhbWJhLTIuMi4zYS8AAQBD8QgwMDQwNzU1ADAwMDE3NjEAMDAwMDE1MggAAwIA/wcAMDc0MjcxMDQw")]
		[InlineData(".corpus/sao", 0, 7251944, 6790273, 0x81d09df4, "sAAAAAABAAAAtfMDCwABDADwSQEAAAAcAAAA1Ke7C7dKOD9rphXawBf3P0Ew0AKZBiK1qpQmMrdL+Jif")]
		[InlineData(".corpus/webster", 0, 41458703, 20139988, 0x52b7fc61, "+sQNClRoZSBQcm9qZWN0IEd1dGVuYmVyZyBFdGV4dCBvZiBUaGUgMTkxMyBXZWJzdGVyIFVuYWJyaWRn")]
		[InlineData(".corpus/xml", 0, 5345280, 1227495, 0x3ae4f54c, "n2VsdHMueG1sAAEASOkxMDA3NzUgACAgIDc2NAgA/w8gIDMzNDc1NyAgNzE3NDM2NjM3MCAgMTIyMDEA")]
		[InlineData(".corpus/x-ray", 0, 8474240, 8390195, 0xcd8a167b, "/w/QAQAQB2wItgAQAAEBEQ6zRlNfQS4zMTk3LmltZwABAA9vQkxLTTE4KAAPBAIAUzgwMDEwMQDzDjgw")]
		public void HighlyCompressedFilesBinaryIdentical(
			string filename, int index, int length, int expectedCompressedLength, uint expectedChecksum, string expectedBytes64)
		{
			var src = Tools.LoadChunk(Tools.FindFile(filename), index, length);

			var dst = LZ4Codec.EncodeHC(src, 0, src.Length, 5);
			var cmp = LZ4Codec.Decode(dst, 0, dst.Length, src.Length);

			string AsHex(uint value) => $"0x{value:x8}";

			Tools.SameBytes(src, cmp);

			//var expectedBytes = Convert.FromBase64String(expectedBytes64);
			//Tools.SameBytes(expectedBytes, dst, expectedBytes.Length);
			//Assert.Equal(expectedCompressedLength, dst.Length);
			//Assert.Equal(AsHex(expectedChecksum), AsHex(Tools.Adler32(dst)));
		}


	}
}
