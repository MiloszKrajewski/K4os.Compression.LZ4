using System;
using System.Runtime.InteropServices;
using System.Runtime.Remoting.Messaging;
using System.Security.Cryptography;
using System.Threading.Tasks;
using K4os.Compression.LZ4.Encoders;
using K4os.Compression.LZ4.Internal;
using K4os.Compression.LZ4.Streams.Abstractions;
using K4os.Compression.LZ4.Streams.Adapters;
using K4os.Compression.LZ4.Streams.Test.Internal;
using TestHelpers;
using Xunit;

namespace K4os.Compression.LZ4.Streams.Test;

public class FrameRountripTests
{
	[Fact]
	public async Task RandomData()
	{
		var settings = new LZ4EncoderSettings();
		var writer = new BufferWriter();
		var adapter = new BufferWriterAdapter(writer);
		var encoder = new FrameEncoder<BufferWriterAdapter>(
			adapter, i => i.CreateEncoder(settings), settings.CreateDescriptor());

		var length = 10 * Mem.M1 + 1337;
		var random = new Random(0);
		var buffer = new byte[length];
		random.NextBytes(buffer);

		var source = Tools.Adler32(buffer);
		await JitterWrite(encoder, random, buffer);

		random.NextBytes(buffer);
		var garbage = Tools.Adler32(buffer);
		Assert.NotEqual(source, garbage);

		var decoder = new FrameDecoder<MemoryReaderAdapter>(
			new MemoryReaderAdapter(writer.WrittenMemory), i => i.CreateDecoder());

		var read = await JitterRead(decoder, random, buffer);
		Assert.Equal(buffer.Length, read);
		var target = Tools.Adler32(buffer);
		Assert.Equal(target, source);
	}

	private static async Task<int> JitterRead(
		IFrameDecoder decoder, Random random, byte[] buffer)
	{
		var length = buffer.Length;
		var position = 0;

		while (true)
		{
			if (length <= 0) break;
			var chunk = random.Next(1, Math.Min(0x1000, length));
			var read = await decoder.ReadManyBytesAsync(buffer.AsMemory(position, chunk), true);
			if (read == 0) break;
			position += read;
			length -= read;
		}

		return position;
	}

	private static async Task JitterWrite(
		IFrameEncoder encoder, Random random, ReadOnlyMemory<byte> buffer)
	{
		var length = buffer.Length;
		var position = 0;

		await encoder.OpenFrameAsync();

		while (length > 0)
		{
			var chunk = random.Next(1, Math.Min(0x1000, length));
			await encoder.WriteManyBytesAsync(buffer.Slice(position, chunk));
			length -= chunk;
			position += chunk;
		}

		await encoder.CloseFrameAsync();
	}
}
