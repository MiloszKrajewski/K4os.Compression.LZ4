using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace K4os.Compression.LZ4.Streams.Abstractions;

/// <summary>
/// Result of async read operation. Returns new state of the stream and number of bytes read.
/// </summary>
/// <param name="Stream">New stream state.</param>
/// <param name="Bytes">Number of bytes read.</param>
/// <typeparam name="TStreamState">Stream state.</typeparam>
public record struct ReadResult<TStreamState>(TStreamState Stream, int Bytes);

/// <summary>
/// Helper methods to create <see cref="ReadResult{TStreamState}"/>
/// </summary>
public static class ReadResult
{
	/// <summary>
	/// Creates read result, composed of new stream state and bytes read.
	/// </summary>
	/// <param name="stream">Stream state.</param>
	/// <param name="bytes">Bytes read.</param>
	/// <typeparam name="TStreamState">Stream state.</typeparam>
	/// <returns>Read result.</returns>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static ReadResult<TStreamState> Create<TStreamState>(
		TStreamState stream, int bytes = 0) =>
		new(stream, bytes);
	
	/// <summary>
	/// Creates read result, composed of new stream state and bytes read.
	/// </summary>
	/// <param name="state">Stream state.</param>
	/// <param name="bytes">Task returning bytes read.</param>
	/// <typeparam name="TStreamState">Stream state.</typeparam>
	/// <returns>Read result.</returns>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static async Task<ReadResult<TStreamState>> Create<TStreamState>(
		TStreamState state, Task<int> bytes) =>
		new(state, await bytes);
}

