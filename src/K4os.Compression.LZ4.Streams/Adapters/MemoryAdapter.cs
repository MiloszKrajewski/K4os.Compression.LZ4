using System;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using K4os.Compression.LZ4.Streams.Abstractions;

namespace K4os.Compression.LZ4.Streams.Adapters;

public readonly struct MemoryAdapter:
	IStreamReader<ReadOnlyMemory<byte>>,
	IStreamReader<Memory<byte>>,
	IStreamWriter<Memory<byte>>
{
	private static int ReadAnyMemory(
		in ReadOnlyMemory<byte> memory, 
		byte[] buffer, int offset, int length)
	{
		length = Math.Min(memory.Length, length);
		if (length <= 0) return 0;

		var source = memory.Span.Slice(0, length);
		var target = buffer.AsSpan(offset, length);
		source.CopyTo(target);
		
		return length;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static int Advance(ref Memory<byte> memory, int length)
	{
		if (length > 0) memory = memory.Slice(length);
		return length;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static int Advance(ref ReadOnlyMemory<byte> memory, int length)
	{
		if (length > 0) memory = memory.Slice(length);
		return length;
	}


	public int Read(
		ref ReadOnlyMemory<byte> memory,
		byte[] buffer, int offset, int length) =>
		Advance(ref memory, ReadAnyMemory(memory, buffer, offset, length));

	public int Read(
		ref Memory<byte> memory,
		byte[] buffer, int offset, int length) =>
		Advance(ref memory, ReadAnyMemory(memory, buffer, offset, length));

	public Task<ReadResult<Memory<byte>>> ReadAsync(
		Memory<byte> memory,
		byte[] buffer, int offset, int length,
		CancellationToken token)
	{
		token.ThrowIfCancellationRequested();
		var bytes = Read(ref memory, buffer, offset, length);
		return Task.FromResult(ReadResult.Some(memory, bytes));
	}

	public Task<ReadResult<ReadOnlyMemory<byte>>> ReadAsync(
		ReadOnlyMemory<byte> memory, 
		byte[] buffer, int offset, int length, 
		CancellationToken token)
	{
		token.ThrowIfCancellationRequested();
		var bytes = Read(ref memory, buffer, offset, length);
		return Task.FromResult(ReadResult.Some(memory, bytes));
	}

	public void Write(
		ref Memory<byte> memory,
		byte[] buffer, int offset, int length)
	{
		if (length <= 0) return;

		if (length > memory.Length)
			throw new ArgumentOutOfRangeException(nameof(length));

		var source = buffer.AsSpan(offset, length);
		var target = memory.Span.Slice(0, length);
		source.CopyTo(target);

		Advance(ref memory, length);
	}

	public Task<Memory<byte>> WriteAsync(
		Memory<byte> memory, byte[] buffer, int offset, int length,
		CancellationToken token)
	{
		token.ThrowIfCancellationRequested();
		Write(ref memory, buffer, offset, length);
		return Task.FromResult(memory);
	}
}
