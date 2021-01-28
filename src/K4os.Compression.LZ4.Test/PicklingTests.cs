using System;
#if NETSTANDARD2_1 || NET5_0
using System.Buffers;
#endif
using System.IO;
using System.Linq;
using K4os.Compression.LZ4;
using K4os.Compression.LZ4.Internal;
using Xunit;

namespace Microsoft.R9.Extensions.Compression.Test
{
	public class PicklingTests
	{
		[Theory]
		[InlineData(0)]
		[InlineData(10)]
		[InlineData(32)]
		[InlineData(200)]
		[InlineData(1337)]
		[InlineData(1337, LZ4Level.L09_HC)]
		[InlineData(0x10000)]
		[InlineData(0x172a5, LZ4Level.L00_FAST)]
		[InlineData(0x172a5, LZ4Level.L09_HC)]
		[InlineData(0x172a5, LZ4Level.L11_OPT)]
		[InlineData(0x172a5, LZ4Level.L12_MAX)]
		[InlineData(Mem.M4, LZ4Level.L12_MAX)]
		public unsafe void PickleLorem(int length, LZ4Level level = LZ4Level.L00_FAST)
		{
			var original = new byte[length];
			Lorem.Fill(original, 0, length);

			var pickled = LZ4Pickler.Pickle(original, level);
			var unpickled = LZ4Pickler.Unpickle(pickled);
			Assert.Equal(original, unpickled);

			fixed (byte* p = original)
			{
				pickled = LZ4Pickler.Pickle(p, original.Length, level);
			}

			fixed (byte* p = pickled)
			{
				unpickled = LZ4Pickler.Unpickle(p, pickled.Length);
			}

			Assert.Equal(original, unpickled);

			var copy = new byte[pickled.Length + 37];
			Array.Copy(pickled, 0, copy, 37, pickled.Length);
			unpickled = LZ4Pickler.Unpickle(copy, 37, pickled.Length);
			Assert.Equal(original, unpickled);

			unpickled.AsSpan().Fill(0);
			LZ4Pickler.Unpickle(pickled.AsSpan(), unpickled.AsSpan());
			Assert.Equal(original, unpickled);
		}

#if NETSTANDARD2_1 || NET5_0
		[Theory]
		[InlineData(0)]
		[InlineData(10)]
		[InlineData(32)]
		[InlineData(200)]
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

			Assert.Throws<ArgumentNullException>(() => LZ4Pickler.Pickle(original, null!, level));
			LZ4Pickler.Pickle(original, pickledWriter, level);
			var pickled = pickledWriter.WrittenSpan;

			Assert.Throws<ArgumentNullException>(() => LZ4Pickler.Unpickle(pickledWriter.WrittenSpan, (IBufferWriter<byte>)null!));
			LZ4Pickler.Unpickle(pickled, unpickledWriter);
			var unpickled = unpickledWriter.WrittenSpan;

			Assert.Equal(original.ToArray(), unpickled.ToArray());
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

			Assert.Equal(original, unpickled);
		}

#if NETSTANDARD2_1 || NET5_0
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

			Assert.Equal(original, unpickled.ToArray());
		}

		[Fact]
		public void Corruption()
		{
			var source = new byte[1234];
			Lorem.Fill(source, 0, source.Length);

			var array = LZ4Pickler.Pickle(source);
			var copy = array.AsSpan().ToArray();
			var output = source.AsSpan().ToArray();

			// pass a buffer that's too short
			Assert.Throws<InvalidDataException>(() => LZ4Pickler.Unpickle(array.AsSpan().Slice(0, 2), output));
			Assert.Throws<InvalidDataException>(() => LZ4Pickler.UnpickledSize(array.AsSpan().Slice(0, 2)));

			// corrupt the version
			array[0] = 0xff;
			Assert.Throws<InvalidDataException>(() => LZ4Pickler.Unpickle(array, output));
			Assert.Throws<InvalidDataException>(() => _ = LZ4Pickler.UnpickledSize(array));

			// corrupt the size
			array[0] = copy[0];
			array[1] = 0xff;
			Assert.Throws<InvalidDataException>(() => LZ4Pickler.Unpickle(array, output));
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
