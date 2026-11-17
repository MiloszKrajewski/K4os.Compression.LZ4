using System;
using Xunit;

namespace K4os.Compression.LZ4.Tests
{
	// https://github.com/MiloszKrajewski/K4os.Compression.LZ4/issues/118
	// Match offset 0 is reserved and denotes a corrupted block.
	// It must be rejected rather than "decoded" into whatever the target buffer contained.
	public class Issue118
	{
		public static readonly TheoryData<byte[], int> CorruptedBlocks = new() {
			// 8 literals, 7-byte match with offset 0, then 5 literals (fast shortcut path)
			{ new byte[] { 0x83, 1, 1, 1, 1, 1, 1, 1, 1, 0, 0, 0x50, 2, 3, 4, 5, 6 }, 20 },
			// 1 literal, 7-byte match with offset 0, then 5 literals
			{ new byte[] { 0x13, 1, 0, 0, 0x50, 2, 3, 4, 5, 6 }, 13 },
			// 16 literals (extended length), 7-byte match with offset 0, then 5 literals
			{
				new byte[] {
					0xF3, 0x01,
					1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1,
					0, 0, 0x50, 2, 3, 4, 5, 6,
				},
				28
			},
			// 8 literals, 24-byte match (extended length) with offset 0, then 5 literals
			{ new byte[] { 0x8F, 1, 1, 1, 1, 1, 1, 1, 1, 0, 0, 0x05, 0x50, 2, 3, 4, 5, 6 }, 37 },
		};

		[Theory]
		[MemberData(nameof(CorruptedBlocks))]
		public void DecodeRejectsOffsetZero(byte[] block, int length)
		{
			foreach (var fill in new byte[] { 0xA5, 0x5A, 0x00 })
			{
				var output = new byte[length];
				output.AsSpan().Fill(fill);
				Assert.True(LZ4Codec.Decode(block.AsSpan(), output.AsSpan()) < 0);
			}
		}

		[Theory]
		[MemberData(nameof(CorruptedBlocks))]
		public void DecodeWithArraysRejectsOffsetZero(byte[] block, int length)
		{
			var output = new byte[length];
			Assert.True(LZ4Codec.Decode(block, 0, block.Length, output, 0, output.Length) < 0);
		}

		[Theory]
		[MemberData(nameof(CorruptedBlocks))]
		public void DecodeWithDictionaryRejectsOffsetZero(byte[] block, int length)
		{
			var dictionary = new byte[1024];
			new Random(118).NextBytes(dictionary);
			var output = new byte[length];
			Assert.True(
				LZ4Codec.Decode(block.AsSpan(), output.AsSpan(), dictionary.AsSpan()) < 0);
		}

		[Theory]
		[MemberData(nameof(CorruptedBlocks))]
		public void PartialDecodeRejectsOffsetZero(byte[] block, int length)
		{
			var output = new byte[length];
			Assert.True(LZ4Codec.PartialDecode(block.AsSpan(), output.AsSpan()) < 0);
		}

		[Fact]
		public void ValidBlockWithSameShapeStillDecodes()
		{
			// same as first corrupted block, but with offset 1 (run of 1s)
			var block = new byte[] { 0x83, 1, 1, 1, 1, 1, 1, 1, 1, 1, 0, 0x50, 2, 3, 4, 5, 6 };
			var output = new byte[20];
			var decoded = LZ4Codec.Decode(block.AsSpan(), output.AsSpan());
			Assert.Equal(20, decoded);
			Assert.Equal(
				new byte[] { 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 2, 3, 4, 5, 6 },
				output);
		}
	}
}
