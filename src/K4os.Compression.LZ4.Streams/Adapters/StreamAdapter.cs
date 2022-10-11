using System;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using K4os.Compression.LZ4.Streams.Abstractions;

namespace K4os.Compression.LZ4.Streams.Adapters;

/// <summary>
/// LZ4 stream reader/writer adapter for <see cref="Stream"/>.
/// Please note, whole <c>K4os.Compression.LZ4.Streams.Adapters</c> namespace should be considered
/// pubternal - exposed as public but still very likely to change.
/// </summary>
public readonly struct StreamAdapter: 
	IStreamReader<EmptyState>, 
	IStreamWriter<EmptyState>
{
	private readonly Stream _stream;

	/// <summary>
	/// Creates new stream adapter for 
	/// </summary>
	/// <param name="stream"></param>
	public StreamAdapter(Stream stream) { _stream = stream; }
	
	/// <inheritdoc />
	public int Read(
		ref EmptyState state,
		byte[] buffer, int offset, int length) =>
		_stream.Read(buffer, offset, length);

	/// <inheritdoc />
	public async Task<ReadResult<EmptyState>> ReadAsync(
		EmptyState state, byte[] buffer, int offset, int length, CancellationToken token) =>
		ReadResult.Create(state, await _stream.ReadAsync(buffer, offset, length, token));

	/// <inheritdoc />
	public void Write(ref EmptyState state, byte[] buffer, int offset, int length) =>
		_stream.Write(buffer, offset, length);

	/// <inheritdoc />
	public async Task<EmptyState> WriteAsync(
		EmptyState state,
		byte[] buffer, int offset, int length,
		CancellationToken token)
	{
		await _stream.WriteAsync(buffer, offset, length, token);
		return state;
	}
	
	/// <inheritdoc />
	public bool CanFlush
	{
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => false;
	}

	/// <inheritdoc />
	public void Flush(ref EmptyState state) { }
	
	/// <inheritdoc />
	public Task<EmptyState> FlushAsync(EmptyState state, CancellationToken token) => 
		Task.FromResult(state);
}
