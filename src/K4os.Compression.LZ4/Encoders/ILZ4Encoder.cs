using System;

namespace K4os.Compression.LZ4.Encoders;

/// <summary>
/// Interface of LZ4 encoder used by LZ4 streams.
/// </summary>
public interface ILZ4Encoder: IDisposable
{
	/// <summary>Block size.</summary>
	int BlockSize { get; }

	/// <summary>Number of bytes read for compression.
	/// Always smaller than <see cref="BlockSize"/></summary>
	int BytesReady { get; }

	/// <summary>Adds bytes to internal buffer. Increases <see cref="BytesReady"/></summary>
	/// <param name="source">Source buffer.</param>
	/// <param name="length">Source buffer length.</param>
	/// <returns>Number of bytes topped up. If this function returns 0 it means that buffer
	/// is full (<see cref="BytesReady"/> equals <see cref="BlockSize"/>) and
	/// <see cref="Encode"/> should be called to flush it.</returns>
	unsafe int Topup(byte* source, int length);

	/// <summary>
	/// Encodes bytes in internal buffer (see: <see cref="BytesReady"/>, <see cref="Topup"/>).
	/// If <paramref name="allowCopy"/> is <c>true</c> then if encoded buffer is bigger than
	/// source buffer source bytes are copied instead. In such case returned length is negative.
	/// </summary>
	/// <param name="target">Target buffer.</param>
	/// <param name="length">Target buffer length.</param>
	/// <param name="allowCopy">Indicates if copying is allowed.</param>
	/// <returns>Length of encoded buffer. Negative if bytes are just copied.</returns>
	unsafe int Encode(byte* target, int length, bool allowCopy);
}