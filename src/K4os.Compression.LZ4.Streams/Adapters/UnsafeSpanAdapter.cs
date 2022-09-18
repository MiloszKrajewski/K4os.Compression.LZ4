#warning return to me
//using System;
//using System.Threading;
//using System.Threading.Tasks;
//using K4os.Compression.LZ4.Internal;
//using K4os.Compression.LZ4.Streams.Abstractions;
//
//namespace K4os.Compression.LZ4.Streams.Adapters;
//
//public unsafe class UnsafeSpanAdapter: 
//	IStreamReader<int>, IStreamWriter<int>
//{
//	private byte* _buffer;
//	private int _length;
//
//	public UnsafeSpanAdapter(byte* source, int length)
//	{
//		_buffer = source;
//		_length = length;
//	}
//
//	public ReadResult<int> Read(int position, byte[] buffer, int offset, int length)
//	{
//		if (length > _length)
//			length = _length;
//
//		if (length <= 0)
//			return ReadResult.None(position);
//
//		fixed (byte* target = buffer)
//			Mem.Copy(target, _buffer, length);
//
//		_buffer += length;
//		_length -= length;
//
//		return length;
//	}
//
//	public Task<int> ReadAsync(byte[] buffer, int offset, int length, CancellationToken token) =>
//		Task.FromResult(Read(buffer, offset, length));
//
//	public void Write(byte[] buffer, int offset, int length)
//	{
//		if (length <= 0)
//			return;
//
//		if (length > _length)
//			throw new ArgumentOutOfRangeException(nameof(length));
//
//		fixed (byte* source = buffer)
//			Mem.Copy(_buffer, source, length);
//
//		_buffer += length;
//		_length -= length;
//	}
//
//	public Task WriteAsync(byte[] buffer, int offset, int length, CancellationToken token)
//	{
//		Write(buffer, offset, length);
//		return LZ4Stream.CompletedTask;
//	}
//}
