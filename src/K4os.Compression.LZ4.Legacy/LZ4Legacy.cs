using System;
using System.IO;

namespace K4os.Compression.LZ4.Legacy
{
	/// <summary>
	/// Utility class with factory methods to create legacy LZ4 (lz4net) compression and decompression streams.
	/// </summary>
	public static class LZ4Legacy
	{
		/// <summary>Initializes a new instance of the <see cref="LZ4Stream" /> class.</summary>
		/// <param name="innerStream">The inner stream.</param>
		/// <param name="highCompression"><c>true</c> if high compression should be used,
		/// <c>false</c> otherwise.</param>
		/// <param name="blockSize">Size of the block.</param>
		/// <param name="leaveOpen">Indicates if inner stream should be left open after compression.</param>
		public static LZ4Stream Encode(
			Stream innerStream,
			bool highCompression = false,
			int blockSize = 1024 * 1024,
			bool leaveOpen = false)
		{
			var compressionFlags =
				(highCompression ? LZ4StreamFlags.HighCompression : LZ4StreamFlags.None) |
				(leaveOpen ? LZ4StreamFlags.IsolateInnerStream : LZ4StreamFlags.None) |
				LZ4StreamFlags.InteractiveRead;
			return new LZ4Stream(innerStream, LZ4StreamMode.Compress, compressionFlags, blockSize);
		}

		/// <summary>Initializes a new instance of the <see cref="LZ4Stream" /> class.</summary>
		/// <param name="innerStream">The inner stream.</param>
		/// <param name="leaveOpen">Indicates if inner stream should be left open after decompression.</param>
		public static LZ4Stream Decode(Stream innerStream, bool leaveOpen = false)
		{
			var compressionFlags =
				leaveOpen ? LZ4StreamFlags.IsolateInnerStream : LZ4StreamFlags.None;
			return new LZ4Stream(innerStream, LZ4StreamMode.Decompress, compressionFlags);
		}
	}
}
