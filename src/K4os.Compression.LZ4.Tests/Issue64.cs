using System;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using K4os.Compression.LZ4.Internal;
using TestHelpers;
using Xunit;

namespace K4os.Compression.LZ4.Tests
{
	public class Issue64
	{
		private static byte[] LoadBytes(string name) =>
			File.ReadAllBytes(Tools.FindFile(@$"./assets/issue64/{name}"));

		[Fact]
		public void ProvidedFileCanBeDecompressed()
		{
			var ascii = new ASCIIEncoding();
			var chunkStart = 20;
			var compressedBlock = LoadBytes("input.dat");
			var expectedOutput = LoadBytes("output.dat");
			var headerValue = compressedBlock.Skip(chunkStart).Take(4).ToArray();
			var outputBuffer = BufferWriter.New(Mem.K64);

			var dictionaryLength = 0;
			var dictionary = default(byte[]);

			while ((16384 > chunkStart) && (!headerValue.SequenceEqual(ascii.GetBytes("bv4$"))))
			{
				var uncompressedSize = compressedBlock.Skip(chunkStart + 4).Take(4).ToArray();
				var compressedSize = compressedBlock.Skip(chunkStart + 8).Take(4).ToArray();

				var uncompressedSizeUInt32 = BitConverter.ToUInt32(uncompressedSize, 0);
				var compressedSizeUInt32 = BitConverter.ToUInt32(compressedSize, 0);
				var decompressedBuffer = new byte[uncompressedSizeUInt32];

				var num = LZ4Codec.Decode(
					compressedBlock.ToArray(), chunkStart + 12, (int)compressedSizeUInt32,
					decompressedBuffer, 0, decompressedBuffer.Length,
					dictionary, 0, dictionaryLength);

				var outputChunk = outputBuffer.GetSpan(num);
				decompressedBuffer.AsSpan(0, num).CopyTo(outputChunk);
				outputBuffer.Advance(num);

				dictionary = decompressedBuffer;
				dictionaryLength = num;

				chunkStart += 12 + (int)compressedSizeUInt32;
				headerValue = compressedBlock.Skip(chunkStart).Take(4).ToArray();
			}

			Tools.SameBytes(expectedOutput.AsSpan(), outputBuffer.WrittenSpan);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static uint Read4(ReadOnlySpan<byte> bytes, int offset = 0) =>
			Unsafe.As<byte, uint>(ref Unsafe.AsRef(in bytes[offset]));

		private static readonly uint Marker =
			Read4(Encoding.ASCII.GetBytes("bv4$").AsSpan());

		[Fact]
		public void SpanIsMuchBetterApproach()
		{
			var inputBuffer = LoadBytes("input.dat").AsSpan().Slice(20);
			var expectedBuffer = LoadBytes("output.dat").AsSpan();

			var outputBuffer = BufferWriter.New();
			var dictionary = outputBuffer.WrittenSpan;

			while (true)
			{
				var header = Read4(inputBuffer);
				if (header == Marker) break;

				var targetSize = (int)Read4(inputBuffer, 4);
				var sourceSize = (int)Read4(inputBuffer, 8);

				var sourceChunk = inputBuffer.Slice(12, sourceSize);
				var targetChunk = outputBuffer.GetSpan(targetSize);

				var decoded = LZ4Codec.Decode(sourceChunk, targetChunk, dictionary);

				dictionary = targetChunk.Slice(0, decoded);
				outputBuffer.Advance(decoded);

				inputBuffer = inputBuffer.Slice(12 + sourceSize);
			}

			Tools.SameBytes(expectedBuffer, outputBuffer.WrittenSpan);
		}
	}
}
