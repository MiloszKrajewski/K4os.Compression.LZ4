using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace K4os.Compression.LZ4.Streams.Abstractions;

public record struct ReadResult<TStreamState>(TStreamState State, int Bytes);

public static class ReadResult
{
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static ReadResult<TStreamState> None<TStreamState>(TStreamState state) => 
		new(state, 0);

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static ReadResult<TStreamState> Some<TStreamState>(
		TStreamState state, int bytes) =>
		new(state, bytes);
	
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static async Task<ReadResult<TStreamState>> Some<TStreamState>(
		TStreamState state, Task<int> bytes) =>
		new(state, await bytes);
}

