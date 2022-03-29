using System;
using System.Threading;
using System.Threading.Tasks;

namespace K4os.Compression.LZ4.Streams.Abstractions
{
	public interface IStreamWriter
	{
		void Write(byte[] buffer, int offset, int length);
		Task WriteAsync(byte[] buffer, int offset, int length, CancellationToken token);
	}
}
