using System;
#if NETSTANDARD2_1
using System.Buffers;
#endif
using K4os.Compression.LZ4.Internal;
using TestHelpers;
using Xunit;

namespace K4os.Compression.LZ4.Test
{
	public class PicklingTests
	{
		[Theory]
		[InlineData(0)]
		[InlineData(10)]
		[InlineData(32)]
		[InlineData(1337)]
		[InlineData(1337, LZ4Level.L09_HC)]
		[InlineData(0x10000)]
		[InlineData(0x172a5, LZ4Level.L00_FAST)]
		[InlineData(0x172a5, LZ4Level.L09_HC)]
		[InlineData(0x172a5, LZ4Level.L11_OPT)]
		[InlineData(0x172a5, LZ4Level.L12_MAX)]
		[InlineData(Mem.M4, LZ4Level.L12_MAX)]
		public void PickleLorem(int length, LZ4Level level = LZ4Level.L00_FAST)
		{
			var original = new byte[length];
			Lorem.Fill(original, 0, length);

			var pickled = LZ4Pickler.Pickle(original, level);
			var unpickled = LZ4Pickler.Unpickle(pickled);

			Tools.SameBytes(original, unpickled);
		}

#if NETSTANDARD2_1
		[Theory]
		[InlineData(0)]
		[InlineData(10)]
		[InlineData(32)]
		[InlineData(1337)]
		[InlineData(1337, LZ4Level.L09_HC)]
		[InlineData(0x10000)]
		[InlineData(0x172a5, LZ4Level.L00_FAST)]
		[InlineData(0x172a5, LZ4Level.L09_HC)]
		[InlineData(0x172a5, LZ4Level.L11_OPT)]
		[InlineData(0x172a5, LZ4Level.L12_MAX)]
		[InlineData(Mem.M4, LZ4Level.L12_MAX)]
		public void PickleLoremWithBufferWriter(int length, LZ4Level level = LZ4Level.L00_FAST)
		{
			var original = new byte[length];
			Lorem.Fill(original, 0, length);

			var pickledWriter = new ArrayBufferWriter<byte>();
			var unpickledWriter = new ArrayBufferWriter<byte>();

			LZ4Pickler.Pickle(original, pickledWriter, level);
			var pickled = pickledWriter.WrittenSpan;

			LZ4Pickler.Unpickle(pickled, unpickledWriter);
			var unpickled = unpickledWriter.WrittenSpan;

			Tools.SameBytes(original, unpickled);
		}
#endif

		[Theory]
		[InlineData(1, 15)]
		[InlineData(2, 1024)]
		[InlineData(3, 1337, LZ4Level.L09_HC)]
		[InlineData(3, 1337, LZ4Level.L12_MAX)]
		[InlineData(4, Mem.K64, LZ4Level.L12_MAX)]
		[InlineData(5, Mem.M4, LZ4Level.L12_MAX)]
		public void PickleEntropy(int seed, int length, LZ4Level level = LZ4Level.L00_FAST)
		{
			var original = new byte[length];
			new Random(seed).NextBytes(original);

			var pickled = LZ4Pickler.Pickle(original, level);
			var unpickled = LZ4Pickler.Unpickle(pickled);

			Tools.SameBytes(original, unpickled);
		}

#if NETSTANDARD2_1
		[Theory]
		[InlineData(1, 15)]
		[InlineData(2, 1024)]
		[InlineData(3, 1337, LZ4Level.L09_HC)]
		[InlineData(3, 1337, LZ4Level.L12_MAX)]
		[InlineData(4, Mem.K64, LZ4Level.L12_MAX)]
		[InlineData(5, Mem.M4, LZ4Level.L12_MAX)]
		public void PickleEntropyWithBufferWriter(int seed, int length, LZ4Level level = LZ4Level.L00_FAST)
		{
			var original = new byte[length];
			new Random(seed).NextBytes(original);

			var pickledWriter = new ArrayBufferWriter<byte>();
			var unpickledWriter = new ArrayBufferWriter<byte>();

			LZ4Pickler.Pickle(original, pickledWriter, level);
			var pickled = pickledWriter.WrittenSpan;

			LZ4Pickler.Unpickle(pickled, unpickledWriter);
			var unpickled = unpickledWriter.WrittenSpan;

			Tools.SameBytes(original, unpickled);
		}
#endif

		[Theory]
		[InlineData(0, 0)]
		[InlineData(0, 1337)]
		[InlineData(1337, 1337)]
		[InlineData(1337, 1)]
		[InlineData(1337, 0)]
		public void PicklingSpansGivesIdenticalResults(int offset, int length)
		{
			var source = new byte[offset + length + offset];
			Lorem.Fill(source, 0, source.Length);

			var array = LZ4Pickler.Pickle(source, offset, length);
			var span = LZ4Pickler.Pickle(source.AsSpan(offset, length));

			Assert.Equal(array, span);

			Assert.Equal(
				LZ4Pickler.Unpickle(array),
				LZ4Pickler.Unpickle(span.AsSpan()));
		}
	}
}
