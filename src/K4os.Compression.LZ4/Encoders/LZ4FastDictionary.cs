using System;
using K4os.Compression.LZ4.Engine;
using K4os.Compression.LZ4.Internal;

namespace K4os.Compression.LZ4.Encoders;

// fast encoder context
using LZ4Context = LL.LZ4_stream_t;

/// <summary>
/// Preset dictionary for <see cref="LZ4FastChainEncoder"/> and <see cref="LZ4ChainDecoder"/>.
/// The dictionary is loaded (hashed) once, when this object is created. Encoders created with it
/// copy the loaded state instead of loading the dictionary again, so it can be shared by any number
/// of encoders and decoders, on any number of threads: it is never modified after construction.
/// Only the last 64KB of the dictionary are used, as LZ4 cannot reference anything further back.
/// </summary>
public unsafe class LZ4FastDictionary: UnmanagedResources
{
	private PinnedMemory _dataPin;
	private PinnedMemory _contextPin;

	private readonly int _length;

	/// <summary>Creates new dictionary.</summary>
	/// <param name="dictionary">Dictionary bytes (only the last 64KB are used).</param>
	public LZ4FastDictionary(ReadOnlySpan<byte> dictionary)
	{
		if (dictionary.Length > Mem.K64)
			dictionary = dictionary.Slice(dictionary.Length - Mem.K64);

		_length = dictionary.Length;
		PinnedMemory.Alloc(out _dataPin, Math.Max(_length, 1), false);
		dictionary.CopyTo(new Span<byte>(_dataPin.Pointer, _length));
		PinnedMemory.Alloc<LZ4Context>(out _contextPin);
		LLxx.LZ4_loadDict(Context, Data, _length);
	}

	/// <summary>Dictionary length (at most 64KB).</summary>
	public int Length => _length;

	/// <summary>Dictionary bytes.</summary>
	public ReadOnlySpan<byte> Span
	{
		get
		{
			ThrowIfDisposed();
			return new ReadOnlySpan<byte>(Data, _length);
		}
	}

	internal byte* Data => _dataPin.Pointer;

	private LZ4Context* Context => _contextPin.Reference<LZ4Context>();

	/// <summary>
	/// Copies dictionary bytes to <paramref name="buffer"/> and the loaded state to
	/// <paramref name="context"/>, so the next block (expected right after the dictionary bytes,
	/// i.e. at <c>buffer + Length</c>) is compressed against the dictionary.
	/// </summary>
	internal void ApplyTo(LZ4Context* context, byte* buffer)
	{
		ThrowIfDisposed();
		Mem.Move(buffer, Data, _length);
		*context = *Context;
		// the loaded state is position independent (hash table holds offsets, not pointers),
		// only the pointer to dictionary itself needs to be adjusted
		var dictEnd = buffer + _length;
		context->dictionary = dictEnd - context->dictSize;
	}

	/// <inheritdoc />
	protected override void ReleaseUnmanaged()
	{
		base.ReleaseUnmanaged();
		_contextPin.Free();
		_dataPin.Free();
	}
}
