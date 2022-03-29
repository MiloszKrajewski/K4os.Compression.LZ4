using System.Threading;
using System.Threading.Tasks;

namespace K4os.Compression.LZ4.Streams.Abstractions;

public interface IStreamReader
{
	int Read(byte[] buffer, int offset, int length);
	Task<int> ReadAsync(byte[] buffer, int offset, int length, CancellationToken token);
}
