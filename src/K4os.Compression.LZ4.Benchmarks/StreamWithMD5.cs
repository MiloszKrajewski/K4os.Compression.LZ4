using System;
using System.IO;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using BenchmarkDotNet.Attributes;
using K4os.Compression.LZ4.Streams;
using TestHelpers;

namespace K4os.Compression.LZ4.Benchmarks
{
	public class ExperimentalBufferStream: Stream
	{
		private readonly Stream _stream;
		private readonly long _length;
		private long _position;

		public ExperimentalBufferStream(Stream stream, long length)
		{
			_stream = stream;
			_length = length;
			_position = 0;
		}

		public override void Flush() { }

		public override int Read(byte[] buffer, int offset, int count)
		{
			var total = 0;
			while (count > 0)
			{
				var read = _stream.Read(buffer, offset, count);
				if (read == 0) return total;

				offset += read;
				count -= read;
				total += read;
				_position += read;
			}

			return total;
		}

		public override long Seek(long offset, SeekOrigin origin) =>
			throw NotImplemented();

		public override void SetLength(long value) =>
			throw NotImplemented();

		public override void Write(byte[] buffer, int offset, int count) =>
			throw NotImplemented();

		public override bool CanRead => true;

		public override bool CanSeek => true;

		public override bool CanWrite => true;

		public override long Length => _length;

		public override long Position
		{
			get => _position;
			set => throw NotImplemented();
		}

		private static Exception NotImplemented([CallerMemberName] string name = null) =>
			new NotImplementedException($"{name} is not implemented");
	}

	public class StreamWithMd5
	{
		private string _compressedFilename;
		private string _sourceFilename;
		private long _sourceLength;

		[GlobalSetup]
		public void Setup()
		{
			_compressedFilename = Path.GetTempFileName();
			_sourceFilename = Tools.FindFile(".corpus/xml");
			using var source = File.OpenRead(_sourceFilename);
			using var target = File.Create(_compressedFilename);
			using var compressed = LZ4Stream.Encode(target);
			source.CopyTo(compressed);
			_sourceLength = source.Length;
		}

		[Benchmark]
		public void Issue()
		{
			using var hasher = MD5.Create();
			using var compressed = File.OpenRead(_compressedFilename);
			using var source = LZ4Stream.Decode(compressed);
			using var hash = new CryptoStream(source, hasher, CryptoStreamMode.Read);
			using var target = new MemoryStream();
			hash.CopyTo(target);
		}
		
		[Benchmark]
		public void IssueWithOptions()
		{
			using var hasher = MD5.Create();
			using var compressed = File.OpenRead(_compressedFilename);
			using var source = LZ4Stream.Decode(compressed, new LZ4DecoderSettings());
			using var hash = new CryptoStream(source, hasher, CryptoStreamMode.Read);
			using var target = new MemoryStream();
			hash.CopyTo(target);
		}


		[Benchmark]
		public void IssueWithWrapper()
		{
			using var hasher = MD5.Create();
			using var compressed = File.OpenRead(_compressedFilename);
			using var source = LZ4Stream.Decode(compressed);
			using var buffer = new ExperimentalBufferStream(source, _sourceLength);
			using var hash = new CryptoStream(buffer, hasher, CryptoStreamMode.Read);
			using var target = new MemoryStream();
			hash.CopyTo(target);
		}

		[Benchmark]
		public void IssueWithBuffer()
		{
			using var hasher = MD5.Create();
			using var compressed = File.OpenRead(_compressedFilename);
			using var source = LZ4Stream.Decode(compressed);
			using var buffer = new BufferedStream(source, 1024 * 1024);
			using var hash = new CryptoStream(buffer, hasher, CryptoStreamMode.Read);
			using var target = new MemoryStream();
			hash.CopyTo(target);
		}
		
		[Benchmark]
		public void IssueWithMemory()
		{
			using var hasher = MD5.Create();
			using var compressed = File.OpenRead(_compressedFilename);
			using var source = LZ4Stream.Decode(compressed);
			using var buffer = new MemoryStream();
			source.CopyTo(buffer);
			buffer.Position = 0;
			using var hash = new CryptoStream(buffer, hasher, CryptoStreamMode.Read);
			using var target = new MemoryStream();
			hash.CopyTo(target);
		}


		[Benchmark]
		public void JustMd5()
		{
			using var hasher = MD5.Create();
			using var source = File.OpenRead(_sourceFilename);
			using var hash = new CryptoStream(source, hasher, CryptoStreamMode.Read);
			using var target = new MemoryStream();
			hash.CopyTo(target);
		}

		[Benchmark]
		public void JustMd5WithBuffer()
		{
			using var hasher = MD5.Create();
			using var source = File.OpenRead(_sourceFilename);
			using var buffer = new BufferedStream(source, 1024 * 1024);
			using var hash = new CryptoStream(buffer, hasher, CryptoStreamMode.Read);
			using var target = new MemoryStream();
			hash.CopyTo(target);
		}

		[Benchmark]
		public void JustDecode()
		{
			using var compressed = File.OpenRead(_compressedFilename);
			using var source = LZ4Stream.Decode(compressed);
			using var target = new MemoryStream();
			source.CopyTo(target);
		}
	}
}
