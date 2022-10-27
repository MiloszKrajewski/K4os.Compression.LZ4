using System;
using System.Runtime.CompilerServices;
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
public unsafe class ByteSpanAdapter:
	IStreamReader<UnsafeByteSpan>,
	IStreamWriter<UnsafeByteSpan>
{
	/// <inheritdoc />
	public int Read(
		ref UnsafeByteSpan state,
		byte[] buffer, int offset, int length)
	{
		var remainingLength = state.Length;
		length = Math.Min(remainingLength, length);
		if (length <= 0) return 0;

		fixed (byte* target = &buffer[offset])
			Mem.Copy(target, state.Ptr, length);

		state.Advance(length);
		
		return length;
	}

	/// <inheritdoc />
	public Task<ReadResult<UnsafeByteSpan>> ReadAsync(
		UnsafeByteSpan state,
		byte[] buffer, int offset, int length,
		CancellationToken token)
	{
		token.ThrowIfCancellationRequested();
		var loaded = Read(ref state, buffer, offset, length);
		return Task.FromResult(ReadResult.Create(state, loaded));
	}

	/// <inheritdoc />
	public void Write(
		ref UnsafeByteSpan state,
		byte[] buffer, int offset, int length)
	{
		if (length <= 0) return;

		var remainingLength = state.Length;
		if (length > remainingLength)
			throw new ArgumentOutOfRangeException(nameof(length));

		fixed (byte* source = &buffer[offset])
			Mem.Copy(state.Ptr, source, length);
		
		state.Advance(length);
	}

	/// <inheritdoc />
	public Task<UnsafeByteSpan> WriteAsync(
		UnsafeByteSpan state, 
		byte[] buffer, int offset, int length,
		CancellationToken token)
	{
		token.ThrowIfCancellationRequested();
		Write(ref state, buffer, offset, length);
		return Task.FromResult(state);
	}
	
	/// <inheritdoc />
	public bool CanFlush
	{
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => false;
	}

	/// <inheritdoc />
	public void Flush(ref UnsafeByteSpan state) { }
	
	/// <inheritdoc />
	public Task<UnsafeByteSpan> FlushAsync(UnsafeByteSpan state, CancellationToken token) => 
		Task.FromResult(state);
}
