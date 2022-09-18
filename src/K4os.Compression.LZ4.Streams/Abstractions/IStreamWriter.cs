using System;
using System.Threading;
using System.Threading.Tasks;

namespace K4os.Compression.LZ4.Streams.Abstractions;

public interface IStreamWriter<TStreamState>
{
	void Write(
		ref TStreamState state, 
		byte[] buffer, int offset, int length);
	
	Task<TStreamState> WriteAsync(
		TStreamState state, 
		byte[] buffer, int offset, int length, 
		CancellationToken token);
}