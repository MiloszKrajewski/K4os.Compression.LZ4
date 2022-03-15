using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace K4os.Compression.LZ4.Streams.NewStreams;

public abstract class LZ4StreamEssentials: Stream
{
	private readonly Stream _inner;
	private readonly bool _leaveOpen;

	private protected LZ4StreamEssentials(Stream inner, bool leaveOpen)
	{
		_inner = inner;
		_leaveOpen = leaveOpen;
	}

	private protected NotImplementedException NotImplemented(string operation) =>
		new($"Feature {operation} has not been implemented in {GetType().Name}");

	private protected InvalidOperationException InvalidOperation(string operation) =>
		new($"Operation {operation} is not allowed for {GetType().Name}");

	private protected static ArgumentException InvalidValue(string description) =>
		new(description);
	
	/// <inheritdoc />
	public override bool CanRead => _inner.CanRead;

	/// <inheritdoc />
	public override bool CanWrite => _inner.CanWrite;

	/// <inheritdoc />
	public override bool CanTimeout => _inner.CanTimeout;

	/// <inheritdoc />
	public override int ReadTimeout
	{
		get => _inner.ReadTimeout;
		set => _inner.ReadTimeout = value;
	}

	/// <inheritdoc />
	public override int WriteTimeout
	{
		get => _inner.WriteTimeout;
		set => _inner.WriteTimeout = value;
	}

	/// <inheritdoc />
	public override bool CanSeek => false;

	/// <inheritdoc />
	public override long Position
	{
		get => throw NotImplemented("GetPosition");
		set => Seek(value, SeekOrigin.Begin);
	}

	/// <inheritdoc />
	public override long Length => 
		throw NotImplemented("GetLength");

	/// <inheritdoc />
	public override void Flush() => 
		_inner.Flush();

	/// <inheritdoc />
	public override Task FlushAsync(CancellationToken token) => 
		_inner.FlushAsync(token);

	/// <inheritdoc />
	public override long Seek(long offset, SeekOrigin origin) =>
		throw InvalidOperation("Seek");

	/// <inheritdoc />
	public override void SetLength(long value) =>
		throw InvalidOperation("SetLength");

	/// <inheritdoc />
	public override int ReadByte() =>
		throw InvalidOperation("ReadByte");

	/// <inheritdoc />
	public override int Read(byte[] buffer, int offset, int count) =>
		throw InvalidOperation("Read");

	/// <inheritdoc />
	public override Task<int> ReadAsync(
		byte[] buffer, int offset, int count, CancellationToken token) =>
		throw InvalidOperation("ReadAsync");
	
	/// <inheritdoc />
	protected override void Dispose(bool disposing)
	{
		if (!_leaveOpen) 
			_inner.Dispose();
		base.Dispose(disposing);
	}
	
	/// <inheritdoc />
	public override void WriteByte(byte value) =>
		throw InvalidOperation("WriteByte");

	/// <inheritdoc />
	public override void Write(byte[] buffer, int offset, int count) =>
		throw InvalidOperation("Write");

	/// <inheritdoc />
	public override Task WriteAsync(
		byte[] buffer, int offset, int count, CancellationToken token) =>
		throw InvalidOperation("WriteAsync");
	
	#if NETSTANDARD2_1_OR_GREATER || NET5_0_OR_GREATER

	/// <inheritdoc />
	public override int Read(Span<byte> buffer) => 
		throw InvalidOperation("Read");

	/// <inheritdoc />
	public override ValueTask<int> ReadAsync(
		Memory<byte> buffer, CancellationToken token = default) =>
		throw InvalidOperation("ReadAsync");

	/// <inheritdoc />
	public override void Write(ReadOnlySpan<byte> buffer) => 
		throw InvalidOperation("Write");

	/// <inheritdoc />
	public override ValueTask WriteAsync(
		ReadOnlyMemory<byte> buffer, CancellationToken token = default) =>
		throw InvalidOperation("WriteAsync");

	/// <inheritdoc />
	public override async ValueTask DisposeAsync()
	{
		if (!_leaveOpen) 
			await _inner.DisposeAsync();
		await base.DisposeAsync();
	}
	
	#endif
}
