using System;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using BenchmarkDotNet.Attributes;
using K4os.Compression.LZ4;
using TestHelpers;

namespace Benchmarks
{
	public class Issue64
	{
		private byte[] _source = null!;

		private static byte[] LoadBytes(string name) =>
			File.ReadAllBytes(Tools.FindFile(@$"./assets/issue64/{name}"));
		
		[GlobalSetup]
		public void Setup() { _source = LoadBytes("input.dat"); }

		[Benchmark(Baseline = true)]
		public void OriginalSolution()
		{
			var ascii = new ASCIIEncoding();
			var chunkStart = 20;
			var compressedBlock = _source;
			var headerValue = compressedBlock.Skip(chunkStart).Take(4).ToArray();
			var outputBuffer = BufferWriter.New();

			var dictionaryLength = 0;
			var dictionary = default(byte[]);

			while (!headerValue.SequenceEqual(ascii.GetBytes("bv4$")))
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

				decompressedBuffer.AsSpan(0, num).CopyTo(outputBuffer.GetSpan(num));
				outputBuffer.Advance(num);

				dictionary = decompressedBuffer;
				dictionaryLength = num;

				chunkStart += 12 + (int)compressedSizeUInt32;
				headerValue = compressedBlock.Skip(chunkStart).Take(4).ToArray();
			}
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static uint Read4(ReadOnlySpan<byte> bytes, int offset = 0) =>
			Unsafe.As<byte, uint>(ref Unsafe.AsRef(in bytes[offset]));

		private static readonly uint Marker =
			Read4(Encoding.ASCII.GetBytes("bv4$").AsSpan());

		[Benchmark]
		public void SpansNoAllocation()
		{
			var inputBuffer = _source.AsSpan().Slice(20);

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
		}
	}
}
