using System.IO;
using BenchmarkDotNet.Attributes;
using K4os.Compression.LZ4.Encoders;
using K4os.Compression.LZ4.Streams;
using K4os.Compression.LZ4.Streams.Test.Internal;
using TestHelpers;

namespace K4os.Compression.LZ4.Benchmarks
{
	public class Decoders
	{
		[Params("mozilla", "webster", "samba")]
		public string FileName { get; set; }

		public Stream Input { get; set; }

		[GlobalSetup]
		public void Setup()
		{
			var temp = Path.GetTempFileName();
			ReferenceLZ4.Encode("-1 -B4", Tools.FindFile($".corpus/{FileName}"), temp);
			Input = new MemoryStream();
			using (var encoded = File.OpenRead(temp)) encoded.CopyTo(Input);
			File.Delete(temp);
		}

		[Benchmark]
		public void ChainDecoderExtraMem()
		{
			Input.Position = 0;
			using (var decoded = new LZ4DecoderStream(
				Input, f => new LZ4ChainDecoder(f.BlockSize, 16), true))
			{
				var buffer = new byte[0x1000];
				while (true)
				{
					var read = decoded.Read(buffer, 0, buffer.Length);
					if (read == 0) break;
				}
			}
		}
		
		[Benchmark]
		public void ChainDecoder()
		{
			Input.Position = 0;
			using (var decoded = new LZ4DecoderStream(
				Input, f => new LZ4ChainDecoder(f.BlockSize, 0), true))
			{
				var buffer = new byte[0x1000];
				while (true)
				{
					var read = decoded.Read(buffer, 0, buffer.Length);
					if (read == 0) break;
				}
			}
		}
		
		[Benchmark]
		public void BlockDecoder()
		{
			Input.Position = 0;
			using (var decoded = new LZ4DecoderStream(
				Input, f => new LZ4BlockDecoder(f.BlockSize), true))
			{
				var buffer = new byte[0x1000];
				while (true)
				{
					var read = decoded.Read(buffer, 0, buffer.Length);
					if (read == 0) break;
				}
			}
		}
	}
}
