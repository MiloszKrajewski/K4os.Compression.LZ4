namespace K4os.Compression.LZ4.Streams.Abstractions;

/// <summary>
/// Stream reader interface. It is an adapter for all stream-like structures.
/// </summary>
/// <typeparam name="TStreamState">Stream state.</typeparam>
public interface IStreamReader<TStreamState>
{
	/// <summary>
	/// Reads at-most <paramref name="length"/> bytes from given <paramref name="state"/>. 
	/// </summary>
	/// <param name="state">Stream state.</param>
	/// <param name="buffer">Buffer to read bytes into.</param>
	/// <param name="offset">Offset in buffer.</param>
	/// <param name="length">Maximum number of bytes to read.</param>
	/// <returns>Number of bytes actually read.</returns>
	int Read(
		ref TStreamState state,
		byte[] buffer, int offset, int length);

	/// <summary>
	/// Reads at-most <paramref name="length"/> bytes from given <paramref name="state"/>. 
	/// </summary>
	/// <param name="state">Stream state.</param>
	/// <param name="buffer">Buffer to read bytes into.</param>
	/// <param name="offset">Offset in buffer.</param>
	/// <param name="length">Maximum number of bytes to read.</param>
	/// <param name="token">Cancellation token.</param>
	/// <returns><see cref="ReadResult{TStreamState}"/> containing new stream state,
	/// and number of bytes actually read..</returns>
	Task<ReadResult<TStreamState>> ReadAsync(
		TStreamState state,
		byte[] buffer, int offset, int length,
		CancellationToken token);
}
