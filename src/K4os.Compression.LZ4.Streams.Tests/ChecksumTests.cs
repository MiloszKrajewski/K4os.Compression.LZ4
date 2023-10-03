using System;
using K4os.Compression.LZ4.Streams.Tests.Internal;
using TestHelpers;
using Xunit;

namespace K4os.Compression.LZ4.Streams.Tests;

public class ChecksumTests
{
	[Theory]
	[InlineData(".corpus/mozilla", true, false)]
	[InlineData(".corpus/mozilla", false, true)]
	[InlineData(".corpus/mozilla", true, true)]
	public void ChecksumIsProduced(string filename, bool block, bool content)
	{
		var source = Tools.FindFile(filename);

		using var target = TempFile.Create();
		using var decoded = TempFile.Create();

		var options = new LZ4Settings
			{ Chaining = true, BlockChecksum = block, ContentChecksum = content };
		TestedLZ4.Encode(source, target.FileName, 1337, options);
		ReferenceLZ4.Decode(target.FileName, decoded.FileName);
		Tools.SameFiles(source, decoded.FileName);
	}

	[Theory]
	[InlineData(".corpus/mozilla", "-B4 -BD -BX")]
	[InlineData(".corpus/mozilla", "-B4 -BD -BX --no-frame-crc")]
	[InlineData(".corpus/mozilla", "-B4 -BD --no-frame-crc")]
	public void ChecksumIsVerified(string filename, string options)
	{
		var source = Tools.FindFile(filename);

		using var target = TempFile.Create();
		using var decoded = TempFile.Create();

		ReferenceLZ4.Encode(options, source, target.FileName);
		TestedLZ4.Decode(target.FileName, decoded.FileName, 1337);
		Tools.SameFiles(source, decoded.FileName);
	}

	[Theory]
	[InlineData(".corpus/mozilla", true, false)]
	[InlineData(".corpus/mozilla", false, true)]
	[InlineData(".corpus/mozilla", true, true)]
	public void ChecksumIsVerifiedRoundtrip(string filename, bool block, bool content)
	{
		var source = Tools.FindFile(filename);

		using var target = TempFile.Create();
		using var decoded = TempFile.Create();

		var options = new LZ4Settings { Chaining = true, BlockChecksum = block, ContentChecksum = content };
		TestedLZ4.Encode(source, target.FileName, 1337, options);
		TestedLZ4.Decode(target.FileName, decoded.FileName, 1337);
		Tools.SameFiles(source, decoded.FileName);
	}
}
