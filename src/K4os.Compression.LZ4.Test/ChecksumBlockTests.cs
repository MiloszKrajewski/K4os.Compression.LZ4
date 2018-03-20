using System;
using Xunit;

namespace K4os.Compression.LZ4.Test
{
	public class ChecksumBlockTests
	{
#if DEBUG
		[Theory(Skip = "Too long")]
#else
		[Theory]
#endif
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
		//[InlineData(".corpus/dickens", 0, 10192446, 6432049, 0x6144792e, "8CMqKlRoZSBQcm9qZWN0IEd1dGVuYmVyZyBFdGV4dCBvZiBBIENoaWxkJ3MgSGlzdG9yeRUA8CJFbmds")]
		//[InlineData(".corpus/mozilla", 0, 51220480, 26375278, 0xaaf353d0, "n21vemlsbGEvAAEASOAgNDA3NTUgACAgMjYwMAgANCAgIAgAAQIA/wgwICA3NDc1NzQyNzYxICAxMDc2")]
		//[InlineData(".corpus/mr", 0, 9970564, 5669219, 0xfcd03aa6, "8CEIAAUACgAAAElTT19JUiAxMDAIAAgAFgAAAE9SSUdJTkFMXFBSSU1BUllcT1RIRVIcAPAKGgAAADEu")]
		//[InlineData(".corpus/nci", 0, 33553445, 5877051, 0xd7feea9a, "8BYxNTU1NDIKUk90Y2xzZXJ2ZTExMTUwMDExMjEyRCAwICAgMC4wAQAQIAEAAwwA8AIxMDQ5NTIxCiAK")]
		//[InlineData(".corpus/ooffice", 0, 6152192, 4228589, 0x13f4365f, "8gNNWpAAAwAAAAQAAAD//wAAuAABABJABwAPAgAK8y7wAAAADh+6DgC0Cc0huAFMzSFUaGlzIHByb2dy")]
		//[InlineData(".corpus/osdb", 0, 10085684, 5223143, 0x4df76a, "8w4DAE8BYAIThAEAAHUlBa4AAAC5za/NhecSTgw2MQIA8T0uMDAJNy8xNy8xOTQ0RmhYVHViOlpRTjVt")]
		//[InlineData(".corpus/reymont", 0, 6627202, 3520792, 0xb81a006b, "8hElUERGLTEuMwozIDAgb2JqIDw8Ci9MZW5ndGggMTUzIAEA8SgKPj4Kc3RyZWFtCjEgMCAwIDEgMjQ0")]
		//[InlineData(".corpus/samba", 0, 21606400, 7897177, 0x8cd79ae9, "73NhbWJhLTIuMi4zYS8AAQBD8AAwMDQwNzU1ADAwMDE3NjEIAEEwMTUyCAADAgD/BwAwNzQyNzEwNDAy")]
		//[InlineData(".corpus/sao", 0, 7251944, 6595675, 0xecfd9131, "sAAAAAABAAAAtfMDCwABDAAAEADwRRwAAADUp7sLt0o4P2umFdrAF/c/QTDQApkGIrWqlCYyt0v4mJ/k")]
		//[InlineData(".corpus/webster", 0, 41458703, 20360352, 0x420439f9, "8BINClRoZSBQcm9qZWN0IEd1dGVuYmVyZyBFdGV4dCBvZiAfAPqfMTkxMyBXZWJzdGVyIFVuYWJyaWRn")]
		//[InlineData(".corpus/xml", 0, 5345280, 1288300, 0x55f0b8a6, "n2VsdHMueG1sAAEASOkxMDA3NzUgACAgIDc2NAgA/w8gIDMzNDc1NyAgNzE3NDM2NjM3MCAgMTIyMDEA")]
		//[InlineData(".corpus/x-ray", 0, 8474240, 8163278, 0xbdf802fe, "/w/QAQAQB2wItgAQAAEBEQ6zRlNfQS4zMTk3LmltZwABAA9vQkxLTTE4KAAPBAIAUzgwMDEwMQDzDjgw")]
		public void CompressedFilesBinaryIdentical(
			string filename, int index, int length, int expectedCompressedLength, uint expectedChecksum, string expectedBytes64)
		{
			var src = Tools.LoadChunk(Tools.FindFile(filename), index, length);

			var dst = LZ4Codec.Encode(src, 0, src.Length, LZ4Level.L00_FAST);
			var cmp = LZ4Codec.Decode(dst, 0, dst.Length, src.Length);

			string AsHex(uint value) => $"0x{value:x8}";

			Tools.SameBytes(src, cmp);

			var expectedBytes = Convert.FromBase64String(expectedBytes64);
			Tools.SameBytes(expectedBytes, dst, expectedBytes.Length);
			Assert.Equal(expectedCompressedLength, dst.Length);
			Assert.Equal(AsHex(expectedChecksum), AsHex(Tools.Adler32(dst)));
		}

#if DEBUG
		[Theory(Skip = "Too long")]
#else
		[Theory]
#endif
		[InlineData(".corpus/dickens", 0, 10192446, 3, 4777698, 0x3dcf78af, "8CMqKlRoZSBQcm9qZWN0IEd1dGVuYmVyZyBFdGV4dCBvZiBBIENoaWxkJ3MgSGlzdG9yeRUA8CJFbmds")]
		[InlineData(".corpus/mozilla", 0, 51220480, 3, 22612180, 0xf068ebda, "n21vemlsbGEvAAEASOAgNDA3NTUgACAgMjYwMAgANCAgIAgAAw0A/wcgNzQ3NTc0Mjc2MSAgMTA3NjUA")]
		[InlineData(".corpus/mr", 0, 9970564, 3, 4645737, 0x165d96a1, "8CEIAAUACgAAAElTT19JUiAxMDAIAAgAFgAAAE9SSUdJTkFMXFBSSU1BUllcT1RIRVIcAPAKGgAAADEu")]
		[InlineData(".corpus/nci", 0, 33553445, 3, 4251597, 0x9f84ce91, "8BYxNTU1NDIKUk90Y2xzZXJ2ZTExMTUwMDExMjEyRCAwICAgMC4wAQAmICAMAP8EMTA0OTUyMQogCiAz")]
		[InlineData(".corpus/ooffice", 0, 6152192, 3, 3607577, 0xa67753d, "8gNNWpAAAwAAAAQAAAD//wAAuAABAC9AAAEAD/Mu8AAAAA4fug4AtAnNIbgBTM0hVGhpcyBwcm9ncmFt")]
		[InlineData(".corpus/osdb", 0, 10085684, 3, 4045536, 0x3a6a79a5, "8w4DAE8BYAIThAEAAHUlBa4AAAC5za/NhecSTgw2MQIA8T0uMDAJNy8xNy8xOTQ0RmhYVHViOlpRTjVt")]
		[InlineData(".corpus/reymont", 0, 6627202, 3, 2428406, 0xfe26edd1, "8hElUERGLTEuMwozIDAgb2JqIDw8Ci9MZW5ndGggMTUzIAEA8SgKPj4Kc3RyZWFtCjEgMCAwIDEgMjQ0")]
		[InlineData(".corpus/samba", 0, 21606400, 3, 6309627, 0xe6526ec0, "73NhbWJhLTIuMi4zYS8AAQBD8AAwMDQwNzU1ADAwMDE3NjEIAGYwMTUyADABAP8IADA3NDI3MTA0MDIw")]
		[InlineData(".corpus/sao", 0, 7251944, 3, 5871276, 0x1e04b294, "xAAAAAABAAAAtfMDAAwAAAQA8EUcAAAA1Ke7C7dKOD9rphXawBf3P0Ew0AKZBiK1qpQmMrdL+Jif5EY/")]
		[InlineData(".corpus/webster", 0, 41458703, 3, 14737393, 0x18019ec6, "8BINClRoZSBQcm9qZWN0IEd1dGVuYmVyZyBFdGV4dCBvZiAfAPCaMTkxMyBXZWJzdGVyIFVuYWJyaWRn")]
		[InlineData(".corpus/xml", 0, 5345280, 3, 852824, 0x895b66f6, "n2VsdHMueG1sAAEASOkxMDA3NzUgACAgIDc2NAgA/xAgIDMzNDc1NyAgNzE3NDM2NjM3MCAgMTIyMDEA")]
		[InlineData(".corpus/x-ray", 0, 8474240, 3, 7202248, 0xab436ed2, "/w/QAQAQB2wItgAQAAEBEQ6zRlNfQS4zMTk3LmltZwABAA9/QkxLTTE4AAEAFlM4MDAxMDEA8g84MC44")]
		[InlineData(".corpus/dickens", 0, 10192446, 9, 4432823, 0x29bb5b4b, "8CMqKlRoZSBQcm9qZWN0IEd1dGVuYmVyZyBFdGV4dCBvZiBBIENoaWxkJ3MgSGlzdG9yeRUA8CJFbmds")]
		[InlineData(".corpus/mozilla", 0, 51220480, 9, 22079369, 0xf58e5d4e, "n21vemlsbGEvAAEASOAgNDA3NTUgACAgMjYwMAgANCAgIAgAAw0A/wcgNzQ3NTc0Mjc2MSAgMTA3NjUA")]
		[InlineData(".corpus/mr", 0, 9970564, 9, 4245211, 0xa298d488, "8CEIAAUACgAAAElTT19JUiAxMDAIAAgAFgAAAE9SSUdJTkFMXFBSSU1BUllcT1RIRVIcAPAKGgAAADEu")]
		[InlineData(".corpus/nci", 0, 33553445, 9, 3673789, 0x2815ae7b, "8BYxNTU1NDIKUk90Y2xzZXJ2ZTExMTUwMDExMjEyRCAwICAgMC4wAQAmICAMAP8EMTA0OTUyMQogCiAz")]
		[InlineData(".corpus/ooffice", 0, 6152192, 9, 3543795, 0xf4e84d7d, "8gNNWpAAAwAAAAQAAAD//wAAuAABAC9AAAEAD/Mu8AAAAA4fug4AtAnNIbgBTM0hVGhpcyBwcm9ncmFt")]
		[InlineData(".corpus/osdb", 0, 10085684, 9, 3977505, 0x70cc0b8d, "8w4DAE8BYAIThAEAAHUlBa4AAAC5za/NhecSTgw2MQIA8T0uMDAJNy8xNy8xOTQ0RmhYVHViOlpRTjVt")]
		[InlineData(".corpus/reymont", 0, 6627202, 9, 2111095, 0x3101de38, "8hElUERGLTEuMwozIDAgb2JqIDw8Ci9MZW5ndGggMTUzIAEA8SgKPj4Kc3RyZWFtCjEgMCAwIDEgMjQ0")]
		[InlineData(".corpus/samba", 0, 21606400, 9, 6139540, 0xc2821d03, "73NhbWJhLTIuMi4zYS8AAQBD8AAwMDQwNzU1ADAwMDE3NjEIAGYwMTUyADABAP8IADA3NDI3MTA0MDIw")]
		[InlineData(".corpus/sao", 0, 7251944, 9, 5735258, 0x86580f55, "xAAAAAABAAAAtfMDAAwAAAQA8EUcAAAA1Ke7C7dKOD9rphXawBf3P0Ew0AKZBiK1qpQmMrdL+Jif5EY/")]
		[InlineData(".corpus/webster", 0, 41458703, 9, 14001448, 0x54c7568d, "8BINClRoZSBQcm9qZWN0IEd1dGVuYmVyZyBFdGV4dCBvZiAfAPCaMTkxMyBXZWJzdGVyIFVuYWJyaWRn")]
		[InlineData(".corpus/xml", 0, 5345280, 9, 770055, 0x4bf9e80d, "n2VsdHMueG1sAAEASOkxMDA3NzUgACAgIDc2NAgA/xAgIDMzNDc1NyAgNzE3NDM2NjM3MCAgMTIyMDEA")]
		[InlineData(".corpus/x-ray", 0, 8474240, 9, 7175001, 0x5c516328, "/w/QAQAQB2wItgAQAAEBEQ6zRlNfQS4zMTk3LmltZwABAA9/QkxLTTE4AAEAFlM4MDAxMDEA8w44MC44")]
		[InlineData(".corpus/dickens", 0, 10192446, 10, 4410756, 0x6005915c, "8CMqKlRoZSBQcm9qZWN0IEd1dGVuYmVyZyBFdGV4dCBvZiBBIENoaWxkJ3MgSGlzdG9yeRUA8CJFbmds")]
		[InlineData(".corpus/mozilla", 0, 51220480, 10, 22155742, 0xb70ac6aa, "n21vemlsbGEvAAEASOAgNDA3NTUgACAgMjYwMAgANCAgIAgAAw0A/wYgNzQ3NTc0Mjc2MSAgMTA3NjUA")]
		[InlineData(".corpus/mr", 0, 9970564, 10, 4215203, 0x3793d8e, "8CEIAAUACgAAAElTT19JUiAxMDAIAAgAFgAAAE9SSUdJTkFMXFBSSU1BUllcT1RIRVIcAPAKGgAAADEu")]
		[InlineData(".corpus/nci", 0, 33553445, 10, 3752129, 0xa3d1ce1b, "8BYxNTU1NDIKUk90Y2xzZXJ2ZTExMTUwMDExMjEyRCAwICAgMC4wAQAmICAMAP8EMTA0OTUyMQogCiAz")]
		[InlineData(".corpus/ooffice", 0, 6152192, 10, 3541849, 0xb0e2ac9f, "8gNNWpAAAwAAAAQAAAD//wAAuAABAC9AAAEAD/Mu8AAAAA4fug4AtAnNIbgBTM0hVGhpcyBwcm9ncmFt")]
		[InlineData(".corpus/osdb", 0, 10085684, 10, 3946363, 0xd08951b9, "8w4DAE8BYAIThAEAAHUlBa4AAAC5za/NhecSTgw2MQIA8T0uMDAJNy8xNy8xOTQ0RmhYVHViOlpRTjVt")]
		[InlineData(".corpus/reymont", 0, 6627202, 10, 2127415, 0xd23ba45a, "8hElUERGLTEuMwozIDAgb2JqIDw8Ci9MZW5ndGggMTUzIAEA8SgKPj4Kc3RyZWFtCjEgMCAwIDEgMjQ0")]
		[InlineData(".corpus/samba", 0, 21606400, 10, 6121585, 0xfdebcb3d, "73NhbWJhLTIuMi4zYS8AAQBD8AIwMDQwNzU1ADAwMDE3NjEAMAkARjUyADABAP8HADA3NDI3MTA0MDIw")]
		[InlineData(".corpus/sao", 0, 7251944, 10, 5681544, 0x33999dd8, "xAAAAAABAAAAtfMDAAwAAAQA8EUcAAAA1Ke7C7dKOD9rphXawBf3P0Ew0AKZBiK1qpQmMrdL+Jif5EY/")]
		[InlineData(".corpus/webster", 0, 41458703, 10, 13950813, 0x625cad37, "8BINClRoZSBQcm9qZWN0IEd1dGVuYmVyZyBFdGV4dCBvZiAfAPCaMTkxMyBXZWJzdGVyIFVuYWJyaWRn")]
		[InlineData(".corpus/xml", 0, 5345280, 10, 777418, 0x8031aa38, "n2VsdHMueG1sAAEASOkxMDA3NzUgACAgIDc2NAgA/w8gIDMzNDc1NyAgNzE3NDM2NjM3MCAgMTIyMDEA")]
		[InlineData(".corpus/x-ray", 0, 8474240, 10, 7172973, 0xae97054b, "/w/QAQAQB2wItgAQAAEBEQ6zRlNfQS4zMTk3LmltZwABAA9/QkxLTTE4AAEAFnM4MDAxMDE4DADzDC44")]
		[InlineData(".corpus/dickens", 0, 10192446, 12, 4376097, 0x93fe23ca, "8CMqKlRoZSBQcm9qZWN0IEd1dGVuYmVyZyBFdGV4dCBvZiBBIENoaWxkJ3MgSGlzdG9yeRUA8CJFbmds")]
		[InlineData(".corpus/mozilla", 0, 51220480, 12, 22014805, 0xccb39eff, "n21vemlsbGEvAAEASOAgNDA3NTUgACAgMjYwMAgANCAgIAgAAw0A/wcgNzQ3NTc0Mjc2MSAgMTA3NjUA")]
		[InlineData(".corpus/mr", 0, 9970564, 12, 4189361, 0x6b65c5fc, "8CEIAAUACgAAAElTT19JUiAxMDAIAAgAFgAAAE9SSUdJTkFMXFBSSU1BUllcT1RIRVIcAPAKGgAAADEu")]
		[InlineData(".corpus/nci", 0, 33553445, 12, 3619014, 0x1c95e5fe, "8BYxNTU1NDIKUk90Y2xzZXJ2ZTExMTUwMDExMjEyRCAwICAgMC4wAQAmICAMAP8EMTA0OTUyMQogCiAz")]
		[InlineData(".corpus/ooffice", 0, 6152192, 12, 3535250, 0xfa843339, "8gNNWpAAAwAAAAQAAAD//wAAuAABAC9AAAEAD/Mu8AAAAA4fug4AtAnNIbgBTM0hVGhpcyBwcm9ncmFt")]
		[InlineData(".corpus/osdb", 0, 10085684, 12, 3946233, 0xd2c31bce, "8w4DAE8BYAIThAEAAHUlBa4AAAC5za/NhecSTgw2MQIA8T0uMDAJNy8xNy8xOTQ0RmhYVHViOlpRTjVt")]
		[InlineData(".corpus/reymont", 0, 6627202, 12, 2063052, 0xdc3640ea, "8hElUERGLTEuMwozIDAgb2JqIDw8Ci9MZW5ndGggMTUzIAEA8SgKPj4Kc3RyZWFtCjEgMCAwIDEgMjQ0")]
		[InlineData(".corpus/samba", 0, 21606400, 12, 6095902, 0x7c2dcd9d, "73NhbWJhLTIuMi4zYS8AAQBD8AIwMDQwNzU1ADAwMDE3NjEAMAkARjUyADABAP8IADA3NDI3MTA0MDIw")]
		[InlineData(".corpus/sao", 0, 7251944, 12, 5668734, 0x25632994, "xAAAAAABAAAAtfMDAAwAAAQA8EUcAAAA1Ke7C7dKOD9rphXawBf3P0Ew0AKZBiK1qpQmMrdL+Jif5EY/")]
		[InlineData(".corpus/webster", 0, 41458703, 12, 13823143, 0x1d513e36, "8BINClRoZSBQcm9qZWN0IEd1dGVuYmVyZyBFdGV4dCBvZiAfAPCaMTkxMyBXZWJzdGVyIFVuYWJyaWRn")]
		[InlineData(".corpus/xml", 0, 5345280, 12, 759893, 0xb41feb8f, "n2VsdHMueG1sAAEASOkxMDA3NzUgACAgIDc2NAgA/xAgIDMzNDc1NyAgNzE3NDM2NjM3MCAgMTIyMDEA")]
		[InlineData(".corpus/x-ray", 0, 8474240, 12, 7172970, 0xc4b20f1d, "/w/QAQAQB2wItgAQAAEBEQ6zRlNfQS4zMTk3LmltZwABAA9/QkxLTTE4AAEAFnM4MDAxMDE4DADzDC44")]
		public void HighlyCompressedFilesBinaryIdentical(
			string filename, int index, int length, int level, int expectedCompressedLength, uint expectedChecksum, string expectedBytes64)
		{
			var src = Tools.LoadChunk(Tools.FindFile(filename), index, length);

			var dst = LZ4Codec.Encode(src, 0, src.Length, (LZ4Level) level);
			var cmp = LZ4Codec.Decode(dst, 0, dst.Length, src.Length);

			string AsHex(uint value) => $"0x{value:x8}";

			Tools.SameBytes(src, cmp);

			var expectedBytes = Convert.FromBase64String(expectedBytes64);
			Tools.SameBytes(expectedBytes, dst, expectedBytes.Length);
			Assert.Equal(expectedCompressedLength, dst.Length);
			Assert.Equal(AsHex(expectedChecksum), AsHex(Tools.Adler32(dst)));
		}
	}
}
