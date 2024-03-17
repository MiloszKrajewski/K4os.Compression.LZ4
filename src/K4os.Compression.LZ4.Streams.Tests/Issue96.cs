using System.Buffers;
using TestHelpers;
using Xunit;

namespace K4os.Compression.LZ4.Streams.Tests;

public class Issue96
{
	private static int EstimateLZ4FrameSize(byte[] source)
	{
		using var memory = new MemoryStream();
		using (var target = LZ4Stream.Encode(memory, leaveOpen: true))
			target.Write(source, 0, source.Length);
		return (int)memory.Length;
	}

	public (byte[] Source, int Expected, int Maximum) PrepareTestCase(int length)
	{
		var source = Lorem.Create(length);
		var expected = EstimateLZ4FrameSize(source);
		var maximum = LZ4Codec.MaximumOutputSize(source.Length);
		return (source, expected, maximum);
	}
	
	[Theory]
	[InlineData(1337)]
	[InlineData(0x10000)]
	public void SequenceToBufferWriter(int length)
	{
		var testCase = PrepareTestCase(length);
		var source = new ReadOnlySequence<byte>(testCase.Source);
		var target = new BufferWriter();
		var actual = LZ4Frame.Encode(source, target).WrittenMemory.Length;
		Assert.Equal(testCase.Expected, actual);
	}
	
	[Theory]
	[InlineData(1337)]
	[InlineData(0x10000)]
	public void SpanToBufferWriter(int length)
	{
		var testCase = PrepareTestCase(length);
		var source = testCase.Source.AsSpan();
		var target = new BufferWriter();
		var actual = LZ4Frame.Encode(source, target).WrittenMemory.Length;
		Assert.Equal(testCase.Expected, actual);
	}
	
	[Theory]
	[InlineData(1337)]
	[InlineData(0x10000)]
	public void SequenceToSpan(int length)
	{
		var testCase = PrepareTestCase(length);
		var source = new ReadOnlySequence<byte>(testCase.Source);
		var target = new byte[testCase.Maximum].AsSpan();
		var actual = LZ4Frame.Encode(source, target);
		Assert.Equal(testCase.Expected, actual);
	}
	
	[Theory]
	[InlineData(1337)]
	[InlineData(0x10000)]
	public void SpanToSpan(int length)
	{
		var testCase = PrepareTestCase(length);
		var source = testCase.Source.AsSpan();
		var target = new byte[testCase.Maximum].AsSpan();
		var actual = LZ4Frame.Encode(source, target);
		Assert.Equal(testCase.Expected, actual);
	}
	
	[Theory]
	[InlineData(1337)]
	[InlineData(0x10000)]
	public void CallbackToSpan(int length)
	{
		var testCase = PrepareTestCase(length);
		var source = testCase.Source;
		var target = new byte[testCase.Maximum].AsSpan();
		var actual = LZ4Frame.Encode(w => w.WriteManyBytes(source), target);
		Assert.Equal(testCase.Expected, actual);
	}
}
