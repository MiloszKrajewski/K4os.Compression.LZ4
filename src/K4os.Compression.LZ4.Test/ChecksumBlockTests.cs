// ReSharper disable StringLiteralTypo
// ReSharper disable CommentTypo

using System;
using K4os.Compression.LZ4.Test.Adapters;
using TestHelpers;
using Xunit;

namespace K4os.Compression.LZ4.Test
{
	public class ChecksumBlockTests
	{
		// Fast32
		[InlineData(4, ".corpus/dickens", 0, 10192446, 0, 6432049, 0x6144792e, "8CMqKlRoZSBQcm9qZWN0IEd1dGVuYmVyZyBFdGV4dCBvZiBBIENoaWxkJ3MgSGlzdG9yeRUA8CJFbmds")]
		[InlineData(4, ".corpus/mozilla", 0, 51220480, 0, 26375278, 0xaaf353d0, "n21vemlsbGEvAAEASOAgNDA3NTUgACAgMjYwMAgANCAgIAgAAQIA/wgwICA3NDc1NzQyNzYxICAxMDc2")]
		[InlineData(4, ".corpus/mr", 0, 9970564, 0, 5669219, 0xfcd03aa6, "8CEIAAUACgAAAElTT19JUiAxMDAIAAgAFgAAAE9SSUdJTkFMXFBSSU1BUllcT1RIRVIcAPAKGgAAADEu")]
		[InlineData(4, ".corpus/nci", 0, 33553445, 0, 5877051, 0xd7feea9a, "8BYxNTU1NDIKUk90Y2xzZXJ2ZTExMTUwMDExMjEyRCAwICAgMC4wAQAQIAEAAwwA8AIxMDQ5NTIxCiAK")]
		[InlineData(4, ".corpus/ooffice", 0, 6152192, 0, 4228589, 0x13f4365f, "8gNNWpAAAwAAAAQAAAD//wAAuAABABJABwAPAgAK8y7wAAAADh+6DgC0Cc0huAFMzSFUaGlzIHByb2dy")]
		[InlineData(4, ".corpus/osdb", 0, 10085684, 0, 5223143, 0x4df76a, "8w4DAE8BYAIThAEAAHUlBa4AAAC5za/NhecSTgw2MQIA8T0uMDAJNy8xNy8xOTQ0RmhYVHViOlpRTjVt")]
		[InlineData(4, ".corpus/reymont", 0, 6627202, 0, 3520792, 0xb81a006b, "8hElUERGLTEuMwozIDAgb2JqIDw8Ci9MZW5ndGggMTUzIAEA8SgKPj4Kc3RyZWFtCjEgMCAwIDEgMjQ0")]
		[InlineData(4, ".corpus/samba", 0, 21606400, 0, 7897177, 0x8cd79ae9, "73NhbWJhLTIuMi4zYS8AAQBD8AAwMDQwNzU1ADAwMDE3NjEIAEEwMTUyCAADAgD/BwAwNzQyNzEwNDAy")]
		[InlineData(4, ".corpus/sao", 0, 7251944, 0, 6595675, 0xecfd9131, "sAAAAAABAAAAtfMDCwABDAAAEADwRRwAAADUp7sLt0o4P2umFdrAF/c/QTDQApkGIrWqlCYyt0v4mJ/k")]
		[InlineData(4, ".corpus/webster", 0, 41458703, 0, 20360352, 0x420439f9, "8BINClRoZSBQcm9qZWN0IEd1dGVuYmVyZyBFdGV4dCBvZiAfAPqfMTkxMyBXZWJzdGVyIFVuYWJyaWRn")]
		[InlineData(4, ".corpus/xml", 0, 5345280, 0, 1288300, 0x55f0b8a6, "n2VsdHMueG1sAAEASOkxMDA3NzUgACAgIDc2NAgA/w8gIDMzNDc1NyAgNzE3NDM2NjM3MCAgMTIyMDEA")]
		[InlineData(4, ".corpus/x-ray", 0, 8474240, 0, 8163278, 0xbdf802fe, "/w/QAQAQB2wItgAQAAEBEQ6zRlNfQS4zMTk3LmltZwABAA9vQkxLTTE4KAAPBAIAUzgwMDEwMQDzDjgw")]
		[Theory]
		public void Fast32(
			int architecture,
			string filename, int index, int length, int level,
			int expectedCompressedLength, uint expectedChecksum, string expectedBytes64)
		{
			TestImpl(
				architecture, 
				filename, index, length, level, 
				expectedCompressedLength, expectedChecksum, expectedBytes64);
		}

		// Fast64
		[InlineData(8, ".corpus/dickens", 0, 10192446, 0, 6428742, 0x17278caf, "+qMqKlRoZSBQcm9qZWN0IEd1dGVuYmVyZyBFdGV4dCBvZiBBIENoaWxkJ3MgSGlzdG9yeSBvZiBFbmds")]
		[InlineData(8, ".corpus/mozilla", 0, 51220480, 0, 26435667, 0x929ed42e, "n21vemlsbGEvAAEASPQGIDQwNzU1IAAgIDI2MDAgACAgICAgCAABAgD/CDAgIDc0NzU3NDI3NjEgIDEw")]
		[InlineData(8, ".corpus/mr", 0, 9970564, 0, 5440937, 0x5d218bd6, "+EgIAAUACgAAAElTT19JUiAxMDAIAAgAFgAAAE9SSUdJTkFMXFBSSU1BUllcT1RIRVIIABYAGgAAADEu")]
		[InlineData(8, ".corpus/nci", 0, 33553445, 0, 5533040, 0x20c1d85f, "9hwxNTU1NDIKUk90Y2xzZXJ2ZTExMTUwMDExMjEyRCAwICAgMC4wMDAwMCAgDAD/BDEwNDk1MjEKIAog")]
		[InlineData(8, ".corpus/ooffice", 0, 6152192, 0, 4338918, 0xf6e8e90, "8gNNWpAAAwAAAAQAAAD//wAAuAABABJABwAPAgAK8y7wAAAADh+6DgC0Cc0huAFMzSFUaGlzIHByb2dy")]
		[InlineData(8, ".corpus/osdb", 0, 10085684, 0, 5256666, 0x6f12d3ea, "8w4DAE8BYAIThAEAAHUlBa4AAAC5za/NhecSTgw2MQIA8T0uMDAJNy8xNy8xOTQ0RmhYVHViOlpRTjVt")]
		[InlineData(8, ".corpus/reymont", 0, 6627202, 0, 3181387, 0x41648906, "8hElUERGLTEuMwozIDAgb2JqIDw8Ci9MZW5ndGggMTUzIAEA8SgKPj4Kc3RyZWFtCjEgMCAwIDEgMjQ0")]
		[InlineData(8, ".corpus/samba", 0, 21606400, 0, 7716839, 0x93086c52, "73NhbWJhLTIuMi4zYS8AAQBD8QgwMDQwNzU1ADAwMDE3NjEAMDAwMDE1MggAAwIA/wcAMDc0MjcxMDQw")]
		[InlineData(8, ".corpus/sao", 0, 7251944, 0, 6790273, 0x81d09df4, "sAAAAAABAAAAtfMDCwABDADwSQEAAAAcAAAA1Ke7C7dKOD9rphXawBf3P0Ew0AKZBiK1qpQmMrdL+Jif")]
		[InlineData(8, ".corpus/webster", 0, 41458703, 0, 20139988, 0x52b7fc61, "+sQNClRoZSBQcm9qZWN0IEd1dGVuYmVyZyBFdGV4dCBvZiBUaGUgMTkxMyBXZWJzdGVyIFVuYWJyaWRn")]
		[InlineData(8, ".corpus/xml", 0, 5345280, 0, 1227495, 0x3ae4f54c, "n2VsdHMueG1sAAEASOkxMDA3NzUgACAgIDc2NAgA/w8gIDMzNDc1NyAgNzE3NDM2NjM3MCAgMTIyMDEA")]
		[InlineData(8, ".corpus/x-ray", 0, 8474240, 0, 8390195, 0xcd8a167b, "/w/QAQAQB2wItgAQAAEBEQ6zRlNfQS4zMTk3LmltZwABAA9vQkxLTTE4KAAPBAIAUzgwMDEwMQDzDjgw")]
		[Theory]
		public void Fast64(
			int architecture,
			string filename, int index, int length, int level,
			int expectedCompressedLength, uint expectedChecksum, string expectedBytes64)
		{
			TestImpl(
				architecture, 
				filename, index, length, level, 
				expectedCompressedLength, expectedChecksum, expectedBytes64);
		}

		// High32
		[InlineData(4, ".corpus/dickens", 0, 10192446, 3, 4777698, 0x3dcf78af, "8CMqKlRoZSBQcm9qZWN0IEd1dGVuYmVyZyBFdGV4dCBvZiBBIENoaWxkJ3MgSGlzdG9yeRUA8CJFbmds")]
		[InlineData(4, ".corpus/mozilla", 0, 51220480, 3, 22612180, 0xf068ebda, "n21vemlsbGEvAAEASOAgNDA3NTUgACAgMjYwMAgANCAgIAgAAw0A/wcgNzQ3NTc0Mjc2MSAgMTA3NjUA")]
		[InlineData(4, ".corpus/mr", 0, 9970564, 3, 4645737, 0x165d96a1, "8CEIAAUACgAAAElTT19JUiAxMDAIAAgAFgAAAE9SSUdJTkFMXFBSSU1BUllcT1RIRVIcAPAKGgAAADEu")]
		[InlineData(4, ".corpus/nci", 0, 33553445, 3, 4251597, 0x9f84ce91, "8BYxNTU1NDIKUk90Y2xzZXJ2ZTExMTUwMDExMjEyRCAwICAgMC4wAQAmICAMAP8EMTA0OTUyMQogCiAz")]
		[InlineData(4, ".corpus/ooffice", 0, 6152192, 3, 3607577, 0xa67753d, "8gNNWpAAAwAAAAQAAAD//wAAuAABAC9AAAEAD/Mu8AAAAA4fug4AtAnNIbgBTM0hVGhpcyBwcm9ncmFt")]
		[InlineData(4, ".corpus/osdb", 0, 10085684, 3, 4045536, 0x3a6a79a5, "8w4DAE8BYAIThAEAAHUlBa4AAAC5za/NhecSTgw2MQIA8T0uMDAJNy8xNy8xOTQ0RmhYVHViOlpRTjVt")]
		[InlineData(4, ".corpus/reymont", 0, 6627202, 3, 2428406, 0xfe26edd1, "8hElUERGLTEuMwozIDAgb2JqIDw8Ci9MZW5ndGggMTUzIAEA8SgKPj4Kc3RyZWFtCjEgMCAwIDEgMjQ0")]
		[InlineData(4, ".corpus/samba", 0, 21606400, 3, 6309627, 0xe6526ec0, "73NhbWJhLTIuMi4zYS8AAQBD8AAwMDQwNzU1ADAwMDE3NjEIAGYwMTUyADABAP8IADA3NDI3MTA0MDIw")]
		[InlineData(4, ".corpus/sao", 0, 7251944, 3, 5871276, 0x1e04b294, "xAAAAAABAAAAtfMDAAwAAAQA8EUcAAAA1Ke7C7dKOD9rphXawBf3P0Ew0AKZBiK1qpQmMrdL+Jif5EY/")]
		[InlineData(4, ".corpus/webster", 0, 41458703, 3, 14737393, 0x18019ec6, "8BINClRoZSBQcm9qZWN0IEd1dGVuYmVyZyBFdGV4dCBvZiAfAPCaMTkxMyBXZWJzdGVyIFVuYWJyaWRn")]
		[InlineData(4, ".corpus/xml", 0, 5345280, 3, 852824, 0x895b66f6, "n2VsdHMueG1sAAEASOkxMDA3NzUgACAgIDc2NAgA/xAgIDMzNDc1NyAgNzE3NDM2NjM3MCAgMTIyMDEA")]
		[InlineData(4, ".corpus/x-ray", 0, 8474240, 3, 7202248, 0xab436ed2, "/w/QAQAQB2wItgAQAAEBEQ6zRlNfQS4zMTk3LmltZwABAA9/QkxLTTE4AAEAFlM4MDAxMDEA8g84MC44")]
		[InlineData(4, ".corpus/dickens", 0, 10192446, 9, 4432823, 0x29bb5b4b, "8CMqKlRoZSBQcm9qZWN0IEd1dGVuYmVyZyBFdGV4dCBvZiBBIENoaWxkJ3MgSGlzdG9yeRUA8CJFbmds")]
		[InlineData(4, ".corpus/mozilla", 0, 51220480, 9, 22078791, 0x90af8101, "n21vemlsbGEvAAEASOAgNDA3NTUgACAgMjYwMAgANCAgIAgAAw0A/wcgNzQ3NTc0Mjc2MSAgMTA3NjUA")]
		[InlineData(4, ".corpus/mr", 0, 9970564, 9, 4245211, 0x8580d2de, "8CEIAAUACgAAAElTT19JUiAxMDAIAAgAFgAAAE9SSUdJTkFMXFBSSU1BUllcT1RIRVIcAPAKGgAAADEu")]
		[InlineData(4, ".corpus/nci", 0, 33553445, 9, 3673771, 0x2228a9f3, "8BYxNTU1NDIKUk90Y2xzZXJ2ZTExMTUwMDExMjEyRCAwICAgMC4wAQAmICAMAP8EMTA0OTUyMQogCiAz")]
		[InlineData(4, ".corpus/ooffice", 0, 6152192, 9, 3543764, 0xc8b15e1a, "8gNNWpAAAwAAAAQAAAD//wAAuAABAC9AAAEAD/Mu8AAAAA4fug4AtAnNIbgBTM0hVGhpcyBwcm9ncmFt")]
		[InlineData(4, ".corpus/osdb", 0, 10085684, 9, 3977505, 0x70cc0b8d, "8w4DAE8BYAIThAEAAHUlBa4AAAC5za/NhecSTgw2MQIA8T0uMDAJNy8xNy8xOTQ0RmhYVHViOlpRTjVt")]
		[InlineData(4, ".corpus/reymont", 0, 6627202, 9, 2111095, 0x3101de38, "8hElUERGLTEuMwozIDAgb2JqIDw8Ci9MZW5ndGggMTUzIAEA8SgKPj4Kc3RyZWFtCjEgMCAwIDEgMjQ0")]
		[InlineData(4, ".corpus/samba", 0, 21606400, 9, 6139489, 0xf4044d6a, "73NhbWJhLTIuMi4zYS8AAQBD8AAwMDQwNzU1ADAwMDE3NjEIAGYwMTUyADABAP8IADA3NDI3MTA0MDIw")]
		[InlineData(4, ".corpus/sao", 0, 7251944, 9, 5735258, 0x86580f55, "xAAAAAABAAAAtfMDAAwAAAQA8EUcAAAA1Ke7C7dKOD9rphXawBf3P0Ew0AKZBiK1qpQmMrdL+Jif5EY/")]
		[InlineData(4, ".corpus/webster", 0, 41458703, 9, 14001448, 0x54c7568d, "8BINClRoZSBQcm9qZWN0IEd1dGVuYmVyZyBFdGV4dCBvZiAfAPCaMTkxMyBXZWJzdGVyIFVuYWJyaWRn")]
		[InlineData(4, ".corpus/xml", 0, 5345280, 9, 770055, 0x4bf9e80d, "n2VsdHMueG1sAAEASOkxMDA3NzUgACAgIDc2NAgA/xAgIDMzNDc1NyAgNzE3NDM2NjM3MCAgMTIyMDEA")]
		[InlineData(4, ".corpus/x-ray", 0, 8474240, 9, 7175001, 0x5c516328, "/w/QAQAQB2wItgAQAAEBEQ6zRlNfQS4zMTk3LmltZwABAA9/QkxLTTE4AAEAFlM4MDAxMDEA8w44MC44")]
		[InlineData(4, ".corpus/dickens", 0, 10192446, 10, 4387799, 0x904e564, "8CMqKlRoZSBQcm9qZWN0IEd1dGVuYmVyZyBFdGV4dCBvZiBBIENoaWxkJ3MgSGlzdG9yeRUA8CJFbmds")]
		[InlineData(4, ".corpus/mozilla", 0, 51220480, 10, 22104093, 0xbf39f588, "n21vemlsbGEvAAEASOAgNDA3NTUgACAgMjYwMAgANCAgIAgAAw0A/wYgNzQ3NTc0Mjc2MSAgMTA3NjUA")]
		[InlineData(4, ".corpus/mr", 0, 9970564, 10, 4211991, 0x889040a1, "8CEIAAUACgAAAElTT19JUiAxMDAIAAgAFgAAAE9SSUdJTkFMXFBSSU1BUllcT1RIRVIcAPAKGgAAADEu")]
		[InlineData(4, ".corpus/nci", 0, 33553445, 10, 3713658, 0x6b055d96, "8BYxNTU1NDIKUk90Y2xzZXJ2ZTExMTUwMDExMjEyRCAwICAgMC4wAQAmICAMAP8EMTA0OTUyMQogCiAz")]
		[InlineData(4, ".corpus/ooffice", 0, 6152192, 10, 3538803, 0xd556bf8f, "8gNNWpAAAwAAAAQAAAD//wAAuAABAC9AAAEAD/Mu8AAAAA4fug4AtAnNIbgBTM0hVGhpcyBwcm9ncmFt")]
		[InlineData(4, ".corpus/osdb", 0, 10085684, 10, 3946371, 0x314954b4, "8w4DAE8BYAIThAEAAHUlBa4AAAC5za/NhecSTgw2MQIA8T0uMDAJNy8xNy8xOTQ0RmhYVHViOlpRTjVt")]
		[InlineData(4, ".corpus/reymont", 0, 6627202, 10, 2090314, 0x12ba7ce5, "8hElUERGLTEuMwozIDAgb2JqIDw8Ci9MZW5ndGggMTUzIAEA8SgKPj4Kc3RyZWFtCjEgMCAwIDEgMjQ0")]
		[InlineData(4, ".corpus/samba", 0, 21606400, 10, 6111537, 0x502128f3, "73NhbWJhLTIuMi4zYS8AAQBD8AIwMDQwNzU1ADAwMDE3NjEAMAkARjUyADABAP8HADA3NDI3MTA0MDIw")]
		[InlineData(4, ".corpus/sao", 0, 7251944, 10, 5675760, 0x632caf1b, "xAAAAAABAAAAtfMDAAwAAAQA8EUcAAAA1Ke7C7dKOD9rphXawBf3P0Ew0AKZBiK1qpQmMrdL+Jif5EY/")]
		[InlineData(4, ".corpus/webster", 0, 41458703, 10, 13874032, 0x4ed55152, "8BINClRoZSBQcm9qZWN0IEd1dGVuYmVyZyBFdGV4dCBvZiAfAPCaMTkxMyBXZWJzdGVyIFVuYWJyaWRn")]
		[InlineData(4, ".corpus/xml", 0, 5345280, 10, 769191, 0xb943ffa7, "n2VsdHMueG1sAAEASOkxMDA3NzUgACAgIDc2NAgA/w8gIDMzNDc1NyAgNzE3NDM2NjM3MCAgMTIyMDEA")]
		[InlineData(4, ".corpus/x-ray", 0, 8474240, 10, 7172973, 0xae97054b, "/w/QAQAQB2wItgAQAAEBEQ6zRlNfQS4zMTk3LmltZwABAA9/QkxLTTE4AAEAFnM4MDAxMDE4DADzDC44")]
		[InlineData(4, ".corpus/dickens", 0, 10192446, 12, 4376097, 0x93fe23ca, "8CMqKlRoZSBQcm9qZWN0IEd1dGVuYmVyZyBFdGV4dCBvZiBBIENoaWxkJ3MgSGlzdG9yeRUA8CJFbmds")]
		[InlineData(4, ".corpus/mozilla", 0, 51220480, 12, 22014250, 0x13c6d8bf, "n21vemlsbGEvAAEASOAgNDA3NTUgACAgMjYwMAgANCAgIAgAAw0A/wcgNzQ3NTc0Mjc2MSAgMTA3NjUA")]
		[InlineData(4, ".corpus/mr", 0, 9970564, 12, 4189363, 0x2c54c457, "8CEIAAUACgAAAElTT19JUiAxMDAIAAgAFgAAAE9SSUdJTkFMXFBSSU1BUllcT1RIRVIcAPAKGgAAADEu")]
		[InlineData(4, ".corpus/nci", 0, 33553445, 12, 3617512, 0x6bdfdff8, "8BYxNTU1NDIKUk90Y2xzZXJ2ZTExMTUwMDExMjEyRCAwICAgMC4wAQAmICAMAP8EMTA0OTUyMQogCiAz")]
		[InlineData(4, ".corpus/ooffice", 0, 6152192, 12, 3535250, 0xfa843339, "8gNNWpAAAwAAAAQAAAD//wAAuAABAC9AAAEAD/Mu8AAAAA4fug4AtAnNIbgBTM0hVGhpcyBwcm9ncmFt")]
		[InlineData(4, ".corpus/osdb", 0, 10085684, 12, 3946233, 0xd2c31bce, "8w4DAE8BYAIThAEAAHUlBa4AAAC5za/NhecSTgw2MQIA8T0uMDAJNy8xNy8xOTQ0RmhYVHViOlpRTjVt")]
		[InlineData(4, ".corpus/reymont", 0, 6627202, 12, 2063052, 0xdc3640ea, "8hElUERGLTEuMwozIDAgb2JqIDw8Ci9MZW5ndGggMTUzIAEA8SgKPj4Kc3RyZWFtCjEgMCAwIDEgMjQ0")]
		[InlineData(4, ".corpus/samba", 0, 21606400, 12, 6095902, 0x7c2dcd9d, "73NhbWJhLTIuMi4zYS8AAQBD8AIwMDQwNzU1ADAwMDE3NjEAMAkARjUyADABAP8IADA3NDI3MTA0MDIw")]
		[InlineData(4, ".corpus/sao", 0, 7251944, 12, 5668734, 0x25632994, "xAAAAAABAAAAtfMDAAwAAAQA8EUcAAAA1Ke7C7dKOD9rphXawBf3P0Ew0AKZBiK1qpQmMrdL+Jif5EY/")]
		[InlineData(4, ".corpus/webster", 0, 41458703, 12, 13823143, 0x1d513e36, "8BINClRoZSBQcm9qZWN0IEd1dGVuYmVyZyBFdGV4dCBvZiAfAPCaMTkxMyBXZWJzdGVyIFVuYWJyaWRn")]
		[InlineData(4, ".corpus/xml", 0, 5345280, 12, 759893, 0xb41feb8f, "n2VsdHMueG1sAAEASOkxMDA3NzUgACAgIDc2NAgA/xAgIDMzNDc1NyAgNzE3NDM2NjM3MCAgMTIyMDEA")]
		[InlineData(4, ".corpus/x-ray", 0, 8474240, 12, 7172970, 0xc4b20f1d, "/w/QAQAQB2wItgAQAAEBEQ6zRlNfQS4zMTk3LmltZwABAA9/QkxLTTE4AAEAFnM4MDAxMDE4DADzDC44")]
		[Theory]
		public void High32(
			int architecture,
			string filename, int index, int length, int level,
			int expectedCompressedLength, uint expectedChecksum, string expectedBytes64)
		{
			TestImpl(
				architecture, 
				filename, index, length, level, 
				expectedCompressedLength, expectedChecksum, expectedBytes64);
		}

		// High64
		[InlineData(8, ".corpus/dickens", 0, 10192446, 3, 4777698, 0x3dcf78af, "8CMqKlRoZSBQcm9qZWN0IEd1dGVuYmVyZyBFdGV4dCBvZiBBIENoaWxkJ3MgSGlzdG9yeRUA8CJFbmds")]
		[InlineData(8, ".corpus/mozilla", 0, 51220480, 3, 22612180, 0xf068ebda, "n21vemlsbGEvAAEASOAgNDA3NTUgACAgMjYwMAgANCAgIAgAAw0A/wcgNzQ3NTc0Mjc2MSAgMTA3NjUA")]
		[InlineData(8, ".corpus/mr", 0, 9970564, 3, 4645737, 0x165d96a1, "8CEIAAUACgAAAElTT19JUiAxMDAIAAgAFgAAAE9SSUdJTkFMXFBSSU1BUllcT1RIRVIcAPAKGgAAADEu")]
		[InlineData(8, ".corpus/nci", 0, 33553445, 3, 4251597, 0x9f84ce91, "8BYxNTU1NDIKUk90Y2xzZXJ2ZTExMTUwMDExMjEyRCAwICAgMC4wAQAmICAMAP8EMTA0OTUyMQogCiAz")]
		[InlineData(8, ".corpus/ooffice", 0, 6152192, 3, 3607577, 0xa67753d, "8gNNWpAAAwAAAAQAAAD//wAAuAABAC9AAAEAD/Mu8AAAAA4fug4AtAnNIbgBTM0hVGhpcyBwcm9ncmFt")]
		[InlineData(8, ".corpus/osdb", 0, 10085684, 3, 4045536, 0x3a6a79a5, "8w4DAE8BYAIThAEAAHUlBa4AAAC5za/NhecSTgw2MQIA8T0uMDAJNy8xNy8xOTQ0RmhYVHViOlpRTjVt")]
		[InlineData(8, ".corpus/reymont", 0, 6627202, 3, 2428406, 0xfe26edd1, "8hElUERGLTEuMwozIDAgb2JqIDw8Ci9MZW5ndGggMTUzIAEA8SgKPj4Kc3RyZWFtCjEgMCAwIDEgMjQ0")]
		[InlineData(8, ".corpus/samba", 0, 21606400, 3, 6309627, 0xe6526ec0, "73NhbWJhLTIuMi4zYS8AAQBD8AAwMDQwNzU1ADAwMDE3NjEIAGYwMTUyADABAP8IADA3NDI3MTA0MDIw")]
		[InlineData(8, ".corpus/sao", 0, 7251944, 3, 5871276, 0x1e04b294, "xAAAAAABAAAAtfMDAAwAAAQA8EUcAAAA1Ke7C7dKOD9rphXawBf3P0Ew0AKZBiK1qpQmMrdL+Jif5EY/")]
		[InlineData(8, ".corpus/webster", 0, 41458703, 3, 14737393, 0x18019ec6, "8BINClRoZSBQcm9qZWN0IEd1dGVuYmVyZyBFdGV4dCBvZiAfAPCaMTkxMyBXZWJzdGVyIFVuYWJyaWRn")]
		[InlineData(8, ".corpus/xml", 0, 5345280, 3, 852824, 0x895b66f6, "n2VsdHMueG1sAAEASOkxMDA3NzUgACAgIDc2NAgA/xAgIDMzNDc1NyAgNzE3NDM2NjM3MCAgMTIyMDEA")]
		[InlineData(8, ".corpus/x-ray", 0, 8474240, 3, 7202248, 0xab436ed2, "/w/QAQAQB2wItgAQAAEBEQ6zRlNfQS4zMTk3LmltZwABAA9/QkxLTTE4AAEAFlM4MDAxMDEA8g84MC44")]
		[InlineData(8, ".corpus/dickens", 0, 10192446, 9, 4432823, 0x29bb5b4b, "8CMqKlRoZSBQcm9qZWN0IEd1dGVuYmVyZyBFdGV4dCBvZiBBIENoaWxkJ3MgSGlzdG9yeRUA8CJFbmds")]
		[InlineData(8, ".corpus/mozilla", 0, 51220480, 9, 22078791, 0x90af8101, "n21vemlsbGEvAAEASOAgNDA3NTUgACAgMjYwMAgANCAgIAgAAw0A/wcgNzQ3NTc0Mjc2MSAgMTA3NjUA")]
		[InlineData(8, ".corpus/mr", 0, 9970564, 9, 4245211, 0x8580d2de, "8CEIAAUACgAAAElTT19JUiAxMDAIAAgAFgAAAE9SSUdJTkFMXFBSSU1BUllcT1RIRVIcAPAKGgAAADEu")]
		[InlineData(8, ".corpus/nci", 0, 33553445, 9, 3673771, 0x2228a9f3, "8BYxNTU1NDIKUk90Y2xzZXJ2ZTExMTUwMDExMjEyRCAwICAgMC4wAQAmICAMAP8EMTA0OTUyMQogCiAz")]
		[InlineData(8, ".corpus/ooffice", 0, 6152192, 9, 3543764, 0xc8b15e1a, "8gNNWpAAAwAAAAQAAAD//wAAuAABAC9AAAEAD/Mu8AAAAA4fug4AtAnNIbgBTM0hVGhpcyBwcm9ncmFt")]
		[InlineData(8, ".corpus/osdb", 0, 10085684, 9, 3977505, 0x70cc0b8d, "8w4DAE8BYAIThAEAAHUlBa4AAAC5za/NhecSTgw2MQIA8T0uMDAJNy8xNy8xOTQ0RmhYVHViOlpRTjVt")]
		[InlineData(8, ".corpus/reymont", 0, 6627202, 9, 2111095, 0x3101de38, "8hElUERGLTEuMwozIDAgb2JqIDw8Ci9MZW5ndGggMTUzIAEA8SgKPj4Kc3RyZWFtCjEgMCAwIDEgMjQ0")]
		[InlineData(8, ".corpus/samba", 0, 21606400, 9, 6139489, 0xf4044d6a, "73NhbWJhLTIuMi4zYS8AAQBD8AAwMDQwNzU1ADAwMDE3NjEIAGYwMTUyADABAP8IADA3NDI3MTA0MDIw")]
		[InlineData(8, ".corpus/sao", 0, 7251944, 9, 5735258, 0x86580f55, "xAAAAAABAAAAtfMDAAwAAAQA8EUcAAAA1Ke7C7dKOD9rphXawBf3P0Ew0AKZBiK1qpQmMrdL+Jif5EY/")]
		[InlineData(8, ".corpus/webster", 0, 41458703, 9, 14001448, 0x54c7568d, "8BINClRoZSBQcm9qZWN0IEd1dGVuYmVyZyBFdGV4dCBvZiAfAPCaMTkxMyBXZWJzdGVyIFVuYWJyaWRn")]
		[InlineData(8, ".corpus/xml", 0, 5345280, 9, 770055, 0x4bf9e80d, "n2VsdHMueG1sAAEASOkxMDA3NzUgACAgIDc2NAgA/xAgIDMzNDc1NyAgNzE3NDM2NjM3MCAgMTIyMDEA")]
		[InlineData(8, ".corpus/x-ray", 0, 8474240, 9, 7175001, 0x5c516328, "/w/QAQAQB2wItgAQAAEBEQ6zRlNfQS4zMTk3LmltZwABAA9/QkxLTTE4AAEAFlM4MDAxMDEA8w44MC44")]
		[InlineData(8, ".corpus/dickens", 0, 10192446, 10, 4387799, 0x904e564, "8CMqKlRoZSBQcm9qZWN0IEd1dGVuYmVyZyBFdGV4dCBvZiBBIENoaWxkJ3MgSGlzdG9yeRUA8CJFbmds")]
		[InlineData(8, ".corpus/mozilla", 0, 51220480, 10, 22104093, 0xbf39f588, "n21vemlsbGEvAAEASOAgNDA3NTUgACAgMjYwMAgANCAgIAgAAw0A/wYgNzQ3NTc0Mjc2MSAgMTA3NjUA")]
		[InlineData(8, ".corpus/mr", 0, 9970564, 10, 4211991, 0x889040a1, "8CEIAAUACgAAAElTT19JUiAxMDAIAAgAFgAAAE9SSUdJTkFMXFBSSU1BUllcT1RIRVIcAPAKGgAAADEu")]
		[InlineData(8, ".corpus/nci", 0, 33553445, 10, 3713658, 0x6b055d96, "8BYxNTU1NDIKUk90Y2xzZXJ2ZTExMTUwMDExMjEyRCAwICAgMC4wAQAmICAMAP8EMTA0OTUyMQogCiAz")]
		[InlineData(8, ".corpus/ooffice", 0, 6152192, 10, 3538803, 0xd556bf8f, "8gNNWpAAAwAAAAQAAAD//wAAuAABAC9AAAEAD/Mu8AAAAA4fug4AtAnNIbgBTM0hVGhpcyBwcm9ncmFt")]
		[InlineData(8, ".corpus/osdb", 0, 10085684, 10, 3946371, 0x314954b4, "8w4DAE8BYAIThAEAAHUlBa4AAAC5za/NhecSTgw2MQIA8T0uMDAJNy8xNy8xOTQ0RmhYVHViOlpRTjVt")]
		[InlineData(8, ".corpus/reymont", 0, 6627202, 10, 2090314, 0x12ba7ce5, "8hElUERGLTEuMwozIDAgb2JqIDw8Ci9MZW5ndGggMTUzIAEA8SgKPj4Kc3RyZWFtCjEgMCAwIDEgMjQ0")]
		[InlineData(8, ".corpus/samba", 0, 21606400, 10, 6111537, 0x502128f3, "73NhbWJhLTIuMi4zYS8AAQBD8AIwMDQwNzU1ADAwMDE3NjEAMAkARjUyADABAP8HADA3NDI3MTA0MDIw")]
		[InlineData(8, ".corpus/sao", 0, 7251944, 10, 5675760, 0x632caf1b, "xAAAAAABAAAAtfMDAAwAAAQA8EUcAAAA1Ke7C7dKOD9rphXawBf3P0Ew0AKZBiK1qpQmMrdL+Jif5EY/")]
		[InlineData(8, ".corpus/webster", 0, 41458703, 10, 13874032, 0x4ed55152, "8BINClRoZSBQcm9qZWN0IEd1dGVuYmVyZyBFdGV4dCBvZiAfAPCaMTkxMyBXZWJzdGVyIFVuYWJyaWRn")]
		[InlineData(8, ".corpus/xml", 0, 5345280, 10, 769191, 0xb943ffa7, "n2VsdHMueG1sAAEASOkxMDA3NzUgACAgIDc2NAgA/w8gIDMzNDc1NyAgNzE3NDM2NjM3MCAgMTIyMDEA")]
		[InlineData(8, ".corpus/x-ray", 0, 8474240, 10, 7172973, 0xae97054b, "/w/QAQAQB2wItgAQAAEBEQ6zRlNfQS4zMTk3LmltZwABAA9/QkxLTTE4AAEAFnM4MDAxMDE4DADzDC44")]
		[InlineData(8, ".corpus/dickens", 0, 10192446, 12, 4376097, 0x93fe23ca, "8CMqKlRoZSBQcm9qZWN0IEd1dGVuYmVyZyBFdGV4dCBvZiBBIENoaWxkJ3MgSGlzdG9yeRUA8CJFbmds")]
		[InlineData(8, ".corpus/mozilla", 0, 51220480, 12, 22014250, 0x13c6d8bf, "n21vemlsbGEvAAEASOAgNDA3NTUgACAgMjYwMAgANCAgIAgAAw0A/wcgNzQ3NTc0Mjc2MSAgMTA3NjUA")]
		[InlineData(8, ".corpus/mr", 0, 9970564, 12, 4189363, 0x2c54c457, "8CEIAAUACgAAAElTT19JUiAxMDAIAAgAFgAAAE9SSUdJTkFMXFBSSU1BUllcT1RIRVIcAPAKGgAAADEu")]
		[InlineData(8, ".corpus/nci", 0, 33553445, 12, 3617512, 0x6bdfdff8, "8BYxNTU1NDIKUk90Y2xzZXJ2ZTExMTUwMDExMjEyRCAwICAgMC4wAQAmICAMAP8EMTA0OTUyMQogCiAz")]
		[InlineData(8, ".corpus/ooffice", 0, 6152192, 12, 3535250, 0xfa843339, "8gNNWpAAAwAAAAQAAAD//wAAuAABAC9AAAEAD/Mu8AAAAA4fug4AtAnNIbgBTM0hVGhpcyBwcm9ncmFt")]
		[InlineData(8, ".corpus/osdb", 0, 10085684, 12, 3946233, 0xd2c31bce, "8w4DAE8BYAIThAEAAHUlBa4AAAC5za/NhecSTgw2MQIA8T0uMDAJNy8xNy8xOTQ0RmhYVHViOlpRTjVt")]
		[InlineData(8, ".corpus/reymont", 0, 6627202, 12, 2063052, 0xdc3640ea, "8hElUERGLTEuMwozIDAgb2JqIDw8Ci9MZW5ndGggMTUzIAEA8SgKPj4Kc3RyZWFtCjEgMCAwIDEgMjQ0")]
		[InlineData(8, ".corpus/samba", 0, 21606400, 12, 6095902, 0x7c2dcd9d, "73NhbWJhLTIuMi4zYS8AAQBD8AIwMDQwNzU1ADAwMDE3NjEAMAkARjUyADABAP8IADA3NDI3MTA0MDIw")]
		[InlineData(8, ".corpus/sao", 0, 7251944, 12, 5668734, 0x25632994, "xAAAAAABAAAAtfMDAAwAAAQA8EUcAAAA1Ke7C7dKOD9rphXawBf3P0Ew0AKZBiK1qpQmMrdL+Jif5EY/")]
		[InlineData(8, ".corpus/webster", 0, 41458703, 12, 13823143, 0x1d513e36, "8BINClRoZSBQcm9qZWN0IEd1dGVuYmVyZyBFdGV4dCBvZiAfAPCaMTkxMyBXZWJzdGVyIFVuYWJyaWRn")]
		[InlineData(8, ".corpus/xml", 0, 5345280, 12, 759893, 0xb41feb8f, "n2VsdHMueG1sAAEASOkxMDA3NzUgACAgIDc2NAgA/xAgIDMzNDc1NyAgNzE3NDM2NjM3MCAgMTIyMDEA")]
		[InlineData(8, ".corpus/x-ray", 0, 8474240, 12, 7172970, 0xc4b20f1d, "/w/QAQAQB2wItgAQAAEBEQ6zRlNfQS4zMTk3LmltZwABAA9/QkxLTTE4AAEAFnM4MDAxMDE4DADzDC44")]
		[Theory]
		public void High64(
			int architecture,
			string filename, int index, int length, int level,
			int expectedCompressedLength, uint expectedChecksum, string expectedBytes64)
		{
			TestImpl(
				architecture, 
				filename, index, length, level, 
				expectedCompressedLength, expectedChecksum, expectedBytes64);
		}

		private static void TestImpl(
			int architecture, string filename, int index, int length, int level,
			int expectedCompressedLength, uint expectedChecksum, string expectedBytes64)
		{
			// we cannot test 64-bit codec when ran in 32-bit environment
			// such tests get reported as successful but actually they are not executed 
			if (architecture > IntPtr.Size)
				return;

			LZ4Codec.EnforceA7 = LZ4Codec.Enforce32 = architecture < IntPtr.Size;

			try
			{
				var src = Tools.LoadChunk(Tools.FindFile(filename), index, length);
				var dst = CurrentLZ4.Encode(src, 0, src.Length, (LZ4Level) level);
				var cmp = CurrentLZ4.Decode(dst, 0, dst.Length, src.Length);

				string AsHex(uint value) => $"0x{value:x8}";

				Tools.SameBytes(src, cmp);

				var expectedBytes = Convert.FromBase64String(expectedBytes64);
				Tools.SameBytes(expectedBytes, dst, expectedBytes.Length);

				Assert.Equal(expectedCompressedLength, dst.Length);
				Assert.Equal(AsHex(expectedChecksum), AsHex(Tools.Adler32(dst)));
			}
			finally
			{
				LZ4Codec.EnforceA7 = LZ4Codec.Enforce32 = false;
			}
		}
	}
}
