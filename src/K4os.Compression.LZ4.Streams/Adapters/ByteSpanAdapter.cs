using System;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using K4os.Compression.LZ4.Streams.Abstractions;

namespace K4os.Compression.LZ4.Streams.Adapters;

/// <summary>
/// LZ4 stream reader/writer adapter for <see cref="UnsafeByteSpan"/>.
/// </summary>
public class ByteSpanAdapter: IStreamReader<int>, IStreamWriter<int>
{
	private readonly UnsafeByteSpan _span;

	/// <summary>
	/// Creates new instance of <see cref="ByteSpanAdapter"/>.
	/// </summary>
	/// <param name="span">Memory span.</param>
	public ByteSpanAdapter(UnsafeByteSpan span) { _span = span; }

	/// <inheritdoc />
	public int Read(
		ref int state,
		byte[] buffer, int offset, int length)
	{
		var remainingLength = _span.Length - state;
		length = Math.Min(remainingLength, length);
		if (length <= 0) return 0;

		_span.Span.Slice(state, length).CopyTo(buffer.AsSpan(offset, length));
		state += length;

		return length;
	}

	/// <inheritdoc />
	public Task<ReadResult<int>> ReadAsync(
		int state,
		byte[] buffer, int offset, int length,
		CancellationToken token)
	{
		token.ThrowIfCancellationRequested();
		var loaded = Read(ref state, buffer, offset, length);
		return Task.FromResult(ReadResult.Create(state, loaded));
	}

	/// <inheritdoc />
	public void Write(
		ref int state,
		byte[] buffer, int offset, int length)
	{
		if (length <= 0) return;

		var remainingLength = _span.Length - state;
		if (length > remainingLength)
			throw new ArgumentOutOfRangeException(nameof(length));

		buffer.AsSpan(offset, length).CopyTo(_span.Span.Slice(state, length));
		state += length;
	}

	/// <inheritdoc />
	public Task<int> WriteAsync(
		int state,
		byte[] buffer, int offset, int length,
		CancellationToken token)
	{
		token.ThrowIfCancellationRequested();
		Write(ref state, buffer, offset, length);
		return Task.FromResult(state);
	}

	/// <inheritdoc />
	public bool CanFlush
	{
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => false;
	}

	/// <inheritdoc />
	public void Flush(ref int state) { }

	/// <inheritdoc />
	public Task<int> FlushAsync(int state, CancellationToken token) =>
		Task.FromResult(state);
}
