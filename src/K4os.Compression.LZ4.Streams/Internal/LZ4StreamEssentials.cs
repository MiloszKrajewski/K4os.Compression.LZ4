#nullable enable

using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace K4os.Compression.LZ4.Streams.Internal;

/// <summary>
/// Base class for all <see cref="Stream"/> compatible adapters.
/// </summary>
/// <typeparam name="T">Type of resource stream adapter if for.</typeparam>
public abstract class LZ4StreamEssentials<T>: Stream
{
	private readonly T _innerResource;
	private readonly bool _doNotDispose;
	private bool _alreadyDisposed;

	/// <summary>
	/// Creates new instance of <see cref="LZ4StreamEssentials{T}"/>.
	/// </summary>
	/// <param name="innerResource">Wrapped resource.</param>
	/// <param name="doNotDispose">Do not dispose inner resource after stream is disposed.</param>
	protected LZ4StreamEssentials(T innerResource, bool doNotDispose)
	{
		_innerResource = innerResource;
		_doNotDispose = doNotDispose;
	}

	/// <summary>Wrapped resource.</summary>
	protected T InnerResource => _innerResource;
    
	private protected NotImplementedException NotImplemented(string operation) =>
		new($"Feature {operation} has not been implemented in {GetType().Name}");

	private protected InvalidOperationException InvalidOperation(string operation) =>
		new($"Operation {operation} is not allowed for {GetType().Name}");

	private protected static ArgumentException InvalidValue(string description) =>
		new(description);

	/// <inheritdoc />
	public override bool CanRead => false;

	/// <inheritdoc />
	public override bool CanWrite => false;

	/// <inheritdoc />
	public override bool CanTimeout => false;

	/// <inheritdoc />
	public override int ReadTimeout
	{
		get => throw NotImplemented("ReadTimeout");
		set => throw NotImplemented("ReadTimeout");
	}

	/// <inheritdoc />
	public override int WriteTimeout
	{
		get => throw NotImplemented("WriteTimeout");
		set => throw NotImplemented("WriteTimeout");
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
	public override void Flush() { }

	/// <inheritdoc />
	public override Task FlushAsync(CancellationToken token) => Task.CompletedTask;

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
		if (ShouldDisposeInner(disposing))
		{
			if (_innerResource is IDisposable disposable)
			{
				disposable.Dispose();
			}

			_alreadyDisposed = true;
		}

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
		if (ShouldDisposeInner())
		{
			if (_innerResource is IAsyncDisposable asyncDisposable)
			{
				await asyncDisposable.DisposeAsync().Weave();
			}
			else if (_innerResource is IDisposable disposable)
			{
				disposable.Dispose();
			}

			_alreadyDisposed = true;
		}

		await base.DisposeAsync().Weave();
	}

	#endif
	
	private bool ShouldDisposeInner(bool disposing = true) => 
		disposing && !_doNotDispose && !_alreadyDisposed;
}
