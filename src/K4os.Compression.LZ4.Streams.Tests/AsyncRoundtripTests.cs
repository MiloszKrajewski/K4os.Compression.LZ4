#if NET6_0_OR_GREATER

using System;
using System.Diagnostics.CodeAnalysis;
using TestHelpers;
using Xunit;

namespace K4os.Compression.LZ4.Streams.Tests;

public class AsyncRoundtripTests
{
	[Fact]
	public async Task WriteThenRead()
	{
		var data = new byte[1337];
		Lorem.Fill(data, 0, data.Length);

		using var temp = TempFile.Create();

		{
			var storage = LZ4Stream.Encode(new AsyncOnlyStream(File.Create(temp.FileName)));
			await storage.WriteAsync(data);
			await storage.DisposeAsync();
		}

		{
			var storage = LZ4Stream.Decode(new AsyncOnlyStream(File.OpenRead(temp.FileName)));
			var read = await storage.ReadAsync(data);
			Assert.Equal(data.Length, read);
			await storage.DisposeAsync();
		}
	}
}

public class AsyncOnlyStream: Stream
{
	private readonly Stream _stream;

	public AsyncOnlyStream(Stream stream) => _stream = stream;

	[DoesNotReturn]
	private static InvalidOperationException NotAllowed() => new("This operation is not allowed");

	public override void Flush() => throw NotAllowed();

	public override int Read(byte[] buffer, int offset, int count) => throw NotAllowed();
	public override int Read(Span<byte> buffer) => throw NotAllowed();
	public override int ReadByte() => throw NotAllowed();

	public override void Write(byte[] buffer, int offset, int count) => throw NotAllowed();
	public override void Write(ReadOnlySpan<byte> buffer) => throw NotAllowed();
	public override void WriteByte(byte value) => throw NotAllowed();

	protected override void Dispose(bool disposing) => throw NotAllowed();
	public override void Close() => throw NotAllowed();

	public override bool CanRead => _stream.CanRead;
	public override bool CanSeek => _stream.CanSeek;
	public override bool CanWrite => _stream.CanWrite;
	public override long Length => _stream.Length;
	public override long Position { get => _stream.Position; set => _stream.Position = value; }
	public override bool CanTimeout => _stream.CanTimeout;

	public override int ReadTimeout
	{
		get => _stream.ReadTimeout;
		set => _stream.ReadTimeout = value;
	}

	public override int WriteTimeout
	{
		get => _stream.WriteTimeout;
		set => _stream.WriteTimeout = value;
	}

	public override long Seek(long offset, SeekOrigin origin) => _stream.Seek(offset, origin);
	public override void SetLength(long value) => _stream.SetLength(value);

	public override ValueTask DisposeAsync() => _stream.DisposeAsync();

	public override Task<int> ReadAsync(
		byte[] buffer, int offset, int count, CancellationToken cancellationToken) =>
		_stream.ReadAsync(buffer, offset, count, cancellationToken);

	public override ValueTask<int> ReadAsync(
		Memory<byte> buffer, CancellationToken cancellationToken = default) =>
		_stream.ReadAsync(buffer, cancellationToken);

	public override Task WriteAsync(
		byte[] buffer, int offset, int count, CancellationToken cancellationToken) =>
		_stream.WriteAsync(buffer, offset, count, cancellationToken);

	// redirect all async methods to underlying stream
	public override ValueTask WriteAsync(
		ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default) =>
		_stream.WriteAsync(buffer, cancellationToken);

	public override Task FlushAsync(CancellationToken cancellationToken) =>
		_stream.FlushAsync(cancellationToken);

	public override Task CopyToAsync(
		Stream destination, int bufferSize, CancellationToken cancellationToken) =>
		_stream.CopyToAsync(destination, bufferSize, cancellationToken);
}

#endif
