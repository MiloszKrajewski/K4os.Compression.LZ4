using System.Threading;
using System.Threading.Tasks;

namespace K4os.Compression.LZ4.Streams.Abstractions;

public interface IStreamReader<TStreamState>
{
	int Read(
		ref TStreamState state,
		byte[] buffer, int offset, int length);

	Task<ReadResult<TStreamState>> ReadAsync(
		TStreamState state,
		byte[] buffer, int offset, int length,
		CancellationToken token);
}
