using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using K4os.Compression.LZ4.Encoders;
using Xunit;
using Xunit.Abstractions;

namespace K4os.Compression.LZ4.Tests
{
	public class LZ4FastDictionaryTests
	{
		private const int BlockSize = 16 * 1024;

		private readonly ITestOutputHelper _output;

		public LZ4FastDictionaryTests(ITestOutputHelper output)
		{
			_output = output;
			LZ4Codec.Enforce32 = false;
		}

		[Theory]
		[InlineData(1, 100)]
		[InlineData(50, 1000)]
		[InlineData(3000, 1000)] // ~300KB: the input buffer wraps several times
		public void RoundtripWithDictionary(int messageCount, int dictionarySize)
		{
			using var dictionary = new LZ4FastDictionary(Messages(1, 1000, dictionarySize));
			var messages = MessageList(2, messageCount);

			using var encoder = new LZ4FastChainEncoder(BlockSize, 0, dictionary);
			using var decoder = new LZ4ChainDecoder(BlockSize, 0, dictionary);
			var blocks = Encode(encoder, messages);

			Assert.Equal(messages, Decode(decoder, blocks));
		}

		[Fact]
		public void DictionaryShouldImproveFirstMessages()
		{
			using var dictionary = new LZ4FastDictionary(Messages(1, 1000, 32 * 1024));
			var messages = MessageList(2, 5);

			using var plain = new LZ4FastChainEncoder(BlockSize, 0);
			using var primed = new LZ4FastChainEncoder(BlockSize, 0, dictionary);
			var plainSize = Encode(plain, messages).Sum(b => b.Length);
			var primedSize = Encode(primed, messages).Sum(b => b.Length);

			Assert.True(primedSize * 4 < plainSize * 3, $"Primed: {primedSize}, plain: {plainSize}");
		}

		[Fact]
		public void DecoderWithoutDictionaryShouldNotDecodeDictionaryBlocks()
		{
			using var dictionary = new LZ4FastDictionary(Messages(1, 1000, 32 * 1024));
			var messages = MessageList(2, 5);
			using var encoder = new LZ4FastChainEncoder(BlockSize, 0, dictionary);
			var blocks = Encode(encoder, messages);

			using var decoder = new LZ4ChainDecoder(BlockSize, 0);
			var failed = false;
			try
			{
				var decoded = Decode(decoder, blocks);
				failed = !decoded.SequenceEqual(messages, ByteArrayComparer.Instance);
			}
			catch (InvalidOperationException)
			{
				failed = true;
			}

			Assert.True(failed);
		}

		[Fact]
		public void SharedDictionaryShouldNotChange()
		{
			var bytes = Messages(1, 1000, 64 * 1024);
			using var dictionary = new LZ4FastDictionary(bytes);
			var dictionaryCopy = dictionary.Span.ToArray();
			var messages = MessageList(2, 500);
			var expected = EncodeFresh(dictionary, messages);

			Parallel.For(0, 32, i =>
			{
				using var encoder = new LZ4FastChainEncoder(BlockSize, 0, dictionary);
				using var decoder = new LZ4ChainDecoder(BlockSize, 0, dictionary);
				var otherMessages = MessageList(100 + i, 300);
				for (var pass = 0; pass < 3; pass++)
				{
					var blocks = Encode(encoder, otherMessages);
					var decoded = Decode(decoder, blocks);
					Assert.Equal(otherMessages, decoded);
					encoder.Reset();
					decoder.Reset();
				}
			});

			Assert.Equal(dictionaryCopy, dictionary.Span.ToArray());
			Assert.Equal(expected, EncodeFresh(dictionary, messages));
		}

		[Fact]
		public void ResetShouldRestoreInitialState()
		{
			using var dictionary = new LZ4FastDictionary(Messages(1, 1000, 16 * 1024));
			var first = MessageList(2, 400);
			var second = MessageList(3, 50);
			var expected = EncodeFresh(dictionary, second);

			using var encoder = new LZ4FastChainEncoder(BlockSize, 0, dictionary);
			using var decoder = new LZ4ChainDecoder(BlockSize, 0, dictionary);
			Decode(decoder, Encode(encoder, first));
			encoder.Reset();
			decoder.Reset();
			var blocks = Encode(encoder, second);

			Assert.Equal(expected, blocks);
			Assert.Equal(second, Decode(decoder, blocks));
		}

		[Fact]
		public void ResetWithoutDictionaryShouldRestoreInitialState()
		{
			var first = MessageList(2, 400);
			var second = MessageList(3, 50);
			using var fresh = new LZ4FastChainEncoder(BlockSize, 0);
			var expected = Encode(fresh, second);

			using var encoder = new LZ4FastChainEncoder(BlockSize, 0);
			using var decoder = new LZ4ChainDecoder(BlockSize, 0, null);
			Decode(decoder, Encode(encoder, first));
			encoder.Reset();
			decoder.Reset();
			var blocks = Encode(encoder, second);

			Assert.Equal(expected, blocks);
			Assert.Equal(second, Decode(decoder, blocks));
		}

		[Theory]
		[InlineData(0)]
		[InlineData(1)]
		[InlineData(3)]
		[InlineData(7)]
		[InlineData(65536)]
		[InlineData(100_000)]
		public void EdgeCaseDictionarySizesShouldRoundtrip(int dictionarySize)
		{
			var bytes = Messages(1, 2000, dictionarySize);
			using var dictionary = new LZ4FastDictionary(bytes);
			var messages = MessageList(2, 200);

			using var encoder = new LZ4FastChainEncoder(BlockSize, 0, dictionary);
			using var decoder = new LZ4ChainDecoder(BlockSize, 0, dictionary);

			Assert.Equal(Math.Min(dictionarySize, 65536), dictionary.Length);
			Assert.Equal(messages, Decode(decoder, Encode(encoder, messages)));
		}

		[Fact]
		public void OnlyDictionaryTailShouldBeUsed()
		{
			var bytes = Messages(1, 2000, 100_000);
			using var full = new LZ4FastDictionary(bytes);
			using var tail = new LZ4FastDictionary(bytes.AsSpan(bytes.Length - 65536));
			var messages = MessageList(2, 50);

			Assert.Equal(tail.Span.ToArray(), full.Span.ToArray());
			Assert.Equal(EncodeFresh(tail, messages), EncodeFresh(full, messages));
		}

		[Fact]
		public void ResetWithDictionaryShouldNotReloadDictionary()
		{
			// Reset copies the loaded state instead of hashing the dictionary again:
			// it should cost a small fraction of loading the dictionary
			var bytes = Messages(1, 2000, 64 * 1024);
			using var dictionary = new LZ4FastDictionary(bytes);
			using var encoder = new LZ4FastChainEncoder(BlockSize, 0, dictionary);
			const int iterations = 2000;

			var resetTime = Best(() => {
				for (var i = 0; i < iterations; i++)
					encoder.Reset();
			});
			var loadTime = Best(() => {
				for (var i = 0; i < iterations; i++)
					new LZ4FastDictionary(bytes).Dispose();
			});

			_output.WriteLine($"Reset: {resetTime.TotalMilliseconds * 1000 / iterations:F2}us, load: {loadTime.TotalMilliseconds * 1000 / iterations:F2}us");
			Assert.True(resetTime.Ticks * 2 < loadTime.Ticks, $"Reset: {resetTime}, load: {loadTime}");
		}

		[Fact]
		public void DictionaryShouldNotSlowDownCompression()
		{
			using var dictionary = new LZ4FastDictionary(Messages(1, 2000, 64 * 1024));
			var messages = MessageList(2, 2000);
			using var plain = new LZ4FastChainEncoder(BlockSize, 0);
			using var primed = new LZ4FastChainEncoder(BlockSize, 0, dictionary);

			var plainTime = Best(() => {
				plain.Reset();
				Encode(plain, messages);
			});
			var primedTime = Best(() => {
				primed.Reset();
				Encode(primed, messages);
			});

			_output.WriteLine($"Primed: {primedTime.TotalMilliseconds:F3}ms, plain: {plainTime.TotalMilliseconds:F3}ms");
			Assert.True(primedTime.Ticks < plainTime.Ticks * 2, $"Primed: {primedTime}, plain: {plainTime}");
		}


		// Helpers

		private static TimeSpan Best(Action action)
		{
			action();
			var best = TimeSpan.MaxValue;
			for (var round = 0; round < 5; round++)
			{
				var stopwatch = System.Diagnostics.Stopwatch.StartNew();
				action();
				if (stopwatch.Elapsed < best)
					best = stopwatch.Elapsed;
			}
			return best;
		}


		private static List<byte[]> EncodeFresh(LZ4FastDictionary dictionary, List<byte[]> messages)
		{
			using var encoder = new LZ4FastChainEncoder(BlockSize, 0, dictionary);
			return Encode(encoder, messages);
		}

		// Every message is encoded into its own block(s), as a streaming transport would
		private static List<byte[]> Encode(LZ4FastChainEncoder encoder, List<byte[]> messages)
		{
			var blocks = new List<byte[]>();
			var target = new byte[LZ4Codec.MaximumOutputSize(encoder.BlockSize)];
			foreach (var message in messages)
			{
				var source = message.AsSpan();
				while (source.Length > 0)
				{
					encoder.TopupAndEncode(
						source, target, true, false, out var loaded, out var encoded);
					Assert.True(loaded > 0 && encoded > 0);
					blocks.Add(target.AsSpan(0, encoded).ToArray());
					source = source.Slice(loaded);
				}
			}
			return blocks;
		}

		private static List<byte[]> Decode(LZ4ChainDecoder decoder, List<byte[]> blocks)
		{
			var output = new List<byte>();
			var target = new byte[decoder.BlockSize];
			foreach (var block in blocks)
			{
				if (!decoder.DecodeAndDrain(block, target, out var decoded))
					throw new InvalidOperationException("Failed to decode block");
				output.AddRange(target.Take(decoded));
			}
			return Split(output.ToArray());
		}

		private static List<byte[]> MessageList(int seed, int count)
		{
			var random = new Random(seed);
			return Enumerable.Range(0, count).Select(_ => Message(random)).ToList();
		}

		private static byte[] Messages(int seed, int count, int length)
		{
			var random = new Random(seed);
			var buffer = new List<byte>();
			while (buffer.Count < length)
				buffer.AddRange(Message(random));
			return buffer.Take(length).ToArray();
		}

		// RPC-like messages: a few method and type names, small numbers, short strings;
		// each one ends with '\n' (it never occurs inside), so the stream can be split back
		private static readonly string[] Methods = {
			"ITodoService.Get", "ITodoService.List", "IAuth.GetUser", "IChatService.PostMessage",
			"$sys.Ok", "$sys.Invalidate", "$sys-c.Ok",
		};

		private static byte[] Message(Random random)
		{
			var text = new StringBuilder()
				.Append("{\"Method\":\"").Append(Methods[random.Next(Methods.Length)])
				.Append("\",\"CallId\":").Append(random.Next(1, 100_000))
				.Append(",\"Headers\":{\"@version\":\"").Append(random.Next(1, 1000)).Append('"')
				.Append("},\"Args\":[\"session-").Append(random.Next(1000))
				.Append("\",").Append(random.Next())
				.Append(",\"Title ").Append(random.Next(100)).Append("\",")
				.Append(random.Next(2) == 0 ? "true" : "false")
				.Append("]}\n");
			return Encoding.UTF8.GetBytes(text.ToString());
		}

		private static List<byte[]> Split(byte[] stream)
		{
			var result = new List<byte[]>();
			var start = 0;
			for (var i = 0; i < stream.Length; i++)
			{
				if (stream[i] != (byte)'\n')
					continue;

				result.Add(stream.AsSpan(start, i + 1 - start).ToArray());
				start = i + 1;
			}
			Assert.Equal(stream.Length, start);
			return result;
		}

		private sealed class ByteArrayComparer: IEqualityComparer<byte[]>
		{
			public static readonly ByteArrayComparer Instance = new();

			public bool Equals(byte[] x, byte[] y) => x.AsSpan().SequenceEqual(y);

			public int GetHashCode(byte[] obj) => obj.Length;
		}
	}
}
