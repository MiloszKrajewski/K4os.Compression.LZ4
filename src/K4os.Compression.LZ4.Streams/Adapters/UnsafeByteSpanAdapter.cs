using System;
using System.Threading;
using System.Threading.Tasks;
using K4os.Compression.LZ4.Internal;
using K4os.Compression.LZ4.Streams.Abstractions;

namespace K4os.Compression.LZ4.Streams.Adapters;

/// <summary>
/// Unsafe version of <see cref="Span{T}"/>. It is unsafe as it stores raw memory pointer
/// so memory it points to must be pinned. It allows reading and writing straight to
/// unmanaged memory but must be used carefully.
/// NOTE: If you don't understand what has been said above - don't use it. Misuse of this
/// struct may lead to unpredictable errors and memory corruption. 
/// </summary>
/// <param name="Bytes">Pointer.</param>
/// <param name="Length">Length.</param>
public unsafe record struct UnsafeByteSpan(UIntPtr Bytes, int Length)
{
	/// <summary>Returns raw memory pointer.</summary>
	public byte* Ptr => (byte*)Bytes.ToPointer();

	/// <summary>Updates span to advance by given number of bytes.</summary>
	/// <param name="offset">Number of bytes to advance by.</param>
	public void Advance(int offset)
	{
		Bytes = UIntPtr.Add(Bytes, offset);
		Length -= offset;
	}
}

/// <summary>
/// LZ4 stream reader/writer adapter for <see cref="UnsafeByteSpan"/>.
/// </summary>
public unsafe class UnsafeByteSpanAdapter:
	IStreamReader<UnsafeByteSpan>,
	IStreamWriter<UnsafeByteSpan>
{
	/// <inheritdoc />
	public int Read(
		ref UnsafeByteSpan stream,
		byte[] buffer, int offset, int length)
	{
		var remainingLength = stream.Length;
		length = Math.Min(remainingLength, length);
		if (length <= 0) return 0;

		fixed (byte* target = &buffer[offset])
			Mem.Copy(target, stream.Ptr, length);

		stream.Advance(length);
		
		return length;
	}

	/// <inheritdoc />
	public Task<ReadResult<UnsafeByteSpan>> ReadAsync(
		UnsafeByteSpan stream,
		byte[] buffer, int offset, int length,
		CancellationToken token)
	{
		token.ThrowIfCancellationRequested();
		var loaded = Read(ref stream, buffer, offset, length);
		return Task.FromResult(ReadResult.Create(stream, loaded));
	}

	/// <inheritdoc />
	public void Write(
		ref UnsafeByteSpan stream,
		byte[] buffer, int offset, int length)
	{
		if (length <= 0) return;

		var remainingLength = stream.Length;
		if (length > remainingLength)
			throw new ArgumentOutOfRangeException(nameof(length));

		fixed (byte* source = &buffer[offset])
			Mem.Copy(stream.Ptr, source, length);
		
		stream.Advance(length);
	}

	/// <inheritdoc />
	public Task<UnsafeByteSpan> WriteAsync(
		UnsafeByteSpan stream, 
		byte[] buffer, int offset, int length, 
		CancellationToken token)
	{
		token.ThrowIfCancellationRequested();
		Write(ref stream, buffer, offset, length);
		return Task.FromResult(stream);
	}
}
