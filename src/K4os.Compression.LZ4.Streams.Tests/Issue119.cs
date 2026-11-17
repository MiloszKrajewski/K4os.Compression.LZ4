using K4os.Hash.xxHash;
using TestHelpers;
using Xunit;

namespace K4os.Compression.LZ4.Streams.Tests;

// https://github.com/MiloszKrajewski/K4os.Compression.LZ4/issues/119
// Reading concatenated frames must not return 0 (end of stream) in the middle of data
// when a read ends exactly at a frame boundary, or when a frame is empty.
public class Issue119
{
	private static byte[] Data(int length)
	{
		var data = new byte[length];
		new Random(length).NextBytes(data);
		return data;
	}

	private static byte[] Frame(byte[] data)
	{
		if (data.Length == 0) return EmptyFrame();

		using var memory = new MemoryStream();
		using (var encoder = LZ4Stream.Encode(memory, leaveOpen: true))
			encoder.Write(data, 0, data.Length);
		return memory.ToArray();
	}

	// header with no blocks, immediately followed by the end mark
	private static byte[] EmptyFrame()
	{
		const byte flg = 0x60; // version 01, independent blocks, no checksums
		const byte bd = 0x40; // 64KB max block size
		var hc = (byte)(XXH32.DigestOf(new[] { flg, bd }, 0, 2) >> 8);
		return new byte[] { 0x04, 0x22, 0x4D, 0x18, flg, bd, hc, 0, 0, 0, 0 };
	}

	private static (byte[] Encoded, byte[] Expected) Concatenate(params int[] lengths)
	{
		var encoded = new MemoryStream();
		var expected = new MemoryStream();
		foreach (var length in lengths)
		{
			var data = Data(length);
			var frame = Frame(data);
			encoded.Write(frame, 0, frame.Length);
			expected.Write(data, 0, data.Length);
		}

		return (encoded.ToArray(), expected.ToArray());
	}

	private static readonly int[] FramesFromIssue = { 81_920, 30_000 };

	private static readonly int[] FramesWithEmpty =
		{ 0, 81_920, 0, 131_072, 70_000, 0, 163_840, 7 };

	[Theory]
	[InlineData(81_920)]
	[InlineData(65_536)]
	[InlineData(100_000)]
	public void ReadDoesNotReturnZeroAtFrameBoundary(int bufferSize)
	{
		var (encoded, expected) = Concatenate(FramesFromIssue);
		using var decoder = LZ4Stream.Decode(new MemoryStream(encoded));
		var buffer = new byte[bufferSize];
		var output = new MemoryStream();

		while (true)
		{
			var read = decoder.Read(buffer, 0, buffer.Length);
			if (read == 0) break;

			output.Write(buffer, 0, read);
		}

		Tools.SameBytes(expected, output.ToArray());
		Assert.Equal(0, decoder.Read(buffer, 0, buffer.Length));
	}

	[Theory]
	[InlineData(81_920)]
	[InlineData(65_536)]
	[InlineData(100_000)]
	public async Task ReadAsyncDoesNotReturnZeroAtFrameBoundary(int bufferSize)
	{
		var (encoded, expected) = Concatenate(FramesFromIssue);
		using var decoder = LZ4Stream.Decode(new MemoryStream(encoded));
		var buffer = new byte[bufferSize];
		var output = new MemoryStream();

		while (true)
		{
			var read = await decoder.ReadAsync(buffer, 0, buffer.Length);
			if (read == 0) break;

			output.Write(buffer, 0, read);
		}

		Tools.SameBytes(expected, output.ToArray());
		Assert.Equal(0, await decoder.ReadAsync(buffer, 0, buffer.Length));
	}

	[Theory]
	[InlineData(4_096)]
	[InlineData(81_920)]
	[InlineData(131_072)]
	public void CopyToReadsAllFrames(int bufferSize)
	{
		var (encoded, expected) = Concatenate(FramesWithEmpty);
		using var decoder = LZ4Stream.Decode(new MemoryStream(encoded));
		var output = new MemoryStream();
		decoder.CopyTo(output, bufferSize);
		Tools.SameBytes(expected, output.ToArray());
	}

	[Theory]
	[InlineData(4_096)]
	[InlineData(81_920)]
	[InlineData(131_072)]
	public async Task CopyToAsyncReadsAllFrames(int bufferSize)
	{
		var (encoded, expected) = Concatenate(FramesWithEmpty);
		using var decoder = LZ4Stream.Decode(new MemoryStream(encoded));
		var output = new MemoryStream();
		await decoder.CopyToAsync(output, bufferSize);
		Tools.SameBytes(expected, output.ToArray());
	}

	[Fact]
	public void NonInteractiveFrameReaderReadsAllFrames()
	{
		var (encoded, expected) = Concatenate(FramesWithEmpty);
		using var reader = LZ4Frame.Decode(new MemoryStream(encoded));
		var buffer = new byte[81_920];
		var output = new MemoryStream();

		while (true)
		{
			var read = reader.ReadManyBytes(buffer.AsSpan());
			if (read == 0) break;

			output.Write(buffer, 0, read);
		}

		Tools.SameBytes(expected, output.ToArray());
	}

	[Fact]
	public void InteractiveReadingStillReturnsZeroAtEveryFrameEnd()
	{
		// interactive mode reports end of each frame consistently, which allows
		// reading frames from a live stream (pipe/socket) one by one
		var lengths = new[] { 81_920, 30_000 };
		var (encoded, _) = Concatenate(lengths);
		using var reader = LZ4Frame.Decode(new MemoryStream(encoded));
		var buffer = new byte[81_920];

		foreach (var length in lengths)
		{
			var total = 0;
			int read;
			while ((read = reader.ReadManyBytes(buffer.AsSpan(), true)) > 0) total += read;
			Assert.Equal(length, total);
		}

		Assert.Equal(0, reader.ReadManyBytes(buffer.AsSpan(), true));
	}

	[Fact]
	public void OnlyEmptyFramesDecodeToNothing()
	{
		var (encoded, _) = Concatenate(0, 0, 0);
		using var decoder = LZ4Stream.Decode(new MemoryStream(encoded));
		var buffer = new byte[1024];
		Assert.Equal(0, decoder.Read(buffer, 0, buffer.Length));
		Assert.Equal(0, decoder.Read(buffer, 0, buffer.Length));
		Assert.Equal(-1, decoder.ReadByte());
	}

	[Fact]
	public void ReadByteCrossesFrameBoundaries()
	{
		var (encoded, expected) = Concatenate(3, 0, 2);
		using var decoder = LZ4Stream.Decode(new MemoryStream(encoded));
		var output = new List<byte>();
		int b;
		while ((b = decoder.ReadByte()) >= 0) output.Add((byte)b);
		Assert.Equal(expected, output.ToArray());
	}
}
