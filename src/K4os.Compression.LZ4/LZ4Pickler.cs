using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Runtime.InteropServices;
using K4os.Compression.LZ4.Internal;

namespace K4os.Compression.LZ4
{
	/// <summary>
	/// Pickling support with LZ4 compression.
	/// </summary>
	public static class LZ4Pickler
	{
		private const byte VersionMask = 0x07;
		private const byte CurrentVersion = 0 & VersionMask; // 3 bits

		/// <summary>Compresses input buffer into self-contained package.</summary>
		/// <param name="source">Input buffer.</param>
		/// <param name="level">Compression level.</param>
		/// <returns>Output buffer.</returns>
		public static byte[] Pickle(byte[] source, LZ4Level level = LZ4Level.L00_FAST) =>
			Pickle(source, 0, source.Length, level);

		/// <summary>Compresses input buffer into self-contained package.</summary>
		/// <param name="source">Input buffer.</param>
		/// <param name="sourceOffset">Input buffer offset.</param>
		/// <param name="sourceLength">Input buffer length.</param>
		/// <param name="level">Compression level.</param>
		/// <returns>Output buffer.</returns>
		public static unsafe byte[] Pickle(
			byte[] source, int sourceOffset, int sourceLength,
			LZ4Level level = LZ4Level.L00_FAST)
		{
			source.Validate(sourceOffset, sourceLength);
			fixed (byte* sourceP = source)
				return Pickle(sourceP + sourceOffset, sourceLength, level);
		}

		/// <summary>Compresses input buffer into self-contained package.</summary>
		/// <param name="source">Input buffer.</param>
		/// <param name="level">Compression level.</param>
		/// <returns>Output buffer.</returns>
		public static unsafe byte[] Pickle(
			ReadOnlySpan<byte> source, LZ4Level level = LZ4Level.L00_FAST)
		{
			var sourceLength = source.Length;
			if (sourceLength <= 0)
				return Mem.Empty;

			fixed (byte* sourceP = &MemoryMarshal.GetReference(source))
				return Pickle(sourceP, sourceLength, level);
		}

		/// <summary>Compresses input buffer into self-contained package.</summary>
		/// <param name="source">Input buffer.</param>
		/// <param name="sourceLength">Length of input data.</param>
		/// <param name="level">Compression level.</param>
		/// <returns>Output buffer.</returns>
		public static unsafe byte[] Pickle(
			byte* source, int sourceLength, LZ4Level level = LZ4Level.L00_FAST)
		{
			if (sourceLength <= 0)
				return Mem.Empty;

			var targetLength = sourceLength - 1;
			var target = (byte*) Mem.Alloc(sourceLength);
			try
			{
				var encodedLength = LZ4Codec.Encode(
					source, sourceLength, target, targetLength, level);

				return encodedLength <= 0
					? PickleV0(source, sourceLength, sourceLength)
					: PickleV0(target, encodedLength, sourceLength);
			}
			finally
			{
				Mem.Free(target);
			}
		}

		/// <summary>Decompresses previously pickled buffer (see: <see cref="LZ4Pickler"/>.</summary>
		/// <param name="source">Input buffer.</param>
		/// <returns>Output buffer.</returns>
		public static byte[] Unpickle(byte[] source) =>
			Unpickle(source, 0, source.Length);

		/// <summary>Decompresses previously pickled buffer (see: <see cref="LZ4Pickler"/>.</summary>
		/// <param name="source">Input buffer.</param>
		/// <param name="sourceOffset">Input buffer offset.</param>
		/// <param name="sourceLength">Input buffer length.</param>
		/// <returns>Output buffer.</returns>
		public static unsafe byte[] Unpickle(
			byte[] source, int sourceOffset, int sourceLength)
		{
			source.Validate(sourceOffset, sourceLength);

			if (sourceLength <= 0)
				return Mem.Empty;

			fixed (byte* sourceP = source)
				return Unpickle(sourceP + sourceOffset, sourceLength);
		}

		/// <summary>Decompresses previously pickled buffer (see: <see cref="LZ4Pickler"/>.</summary>
		/// <param name="source">Input buffer.</param>
		/// <returns>Output buffer.</returns>
		public static unsafe byte[] Unpickle(ReadOnlySpan<byte> source)
		{
			var sourceLength = source.Length;
			if (sourceLength <= 0)
				return Mem.Empty;

			fixed (byte* sourceP = &MemoryMarshal.GetReference(source))
				return Unpickle(sourceP, source.Length);
		}

		/// <summary>Decompresses previously pickled buffer (see: <see cref="LZ4Pickler"/>.</summary>
		/// <param name="source">Input buffer.</param>
		/// <param name="sourceLength">Input buffer length.</param>
		/// <returns>Output buffer.</returns>
		public static unsafe byte[] Unpickle(byte* source, int sourceLength)
		{
			if (sourceLength <= 0)
				return Mem.Empty;

			var flags = *source;
			var version = flags & VersionMask; // 3 bits

			if (version == 0)
				return UnpickleV0(flags, source + 1, sourceLength - 1);

			throw new InvalidDataException($"Pickle version {version} is not supported");
		}

		private static Exception CorruptedPickle(string message) =>
			new InvalidDataException($"Pickle is corrupted: {message}");

		[SuppressMessage("ReSharper", "IdentifierTypo")]
		private static unsafe byte[] PickleV0(
			byte* target, int targetLength, int sourceLength)
		{
			var diff = sourceLength - targetLength;
			var llen = diff == 0 ? 0 : diff < 0x100 ? 1 : diff < 0x10000 ? 2 : 4;
			var result = new byte[targetLength + 1 + llen];

			fixed (byte* resultP = result)
			{
				var llenFlags = llen == 4 ? 3 : llen; // 2 bits
				var flags = (byte) ((llenFlags << 6) | CurrentVersion);
				Mem.Poke1(resultP + 0, flags);
				if (llen == 1) Mem.Poke1(resultP + 1, (byte) diff);
				else if (llen == 2) Mem.Poke2(resultP + 1, (ushort) diff);
				else if (llen == 4) Mem.Poke4(resultP + 1, (uint) diff);
				Mem.Move(resultP + llen + 1, target, targetLength);
			}

			return result;
		}

		private static unsafe byte[] UnpickleV0(
			byte flags, byte* source, int sourceLength)
		{
			// ReSharper disable once IdentifierTypo
			var llen = (flags >> 6) & 0x03; // 2 bits
			if (llen == 3) llen = 4;

			if (sourceLength < llen)
				throw CorruptedPickle("Source buffer is too small.");

			var diff = (int) (
				llen == 0 ? 0 :
				llen == 1 ? *source :
				llen == 2 ? *(ushort*) source :
				llen == 4 ? *(uint*) source :
				throw CorruptedPickle("Unexpected length descriptor.")
			);
			source += llen;
			sourceLength -= llen;
			var targetLength = sourceLength + diff;

			var target = new byte[targetLength];
			fixed (byte* targetP = target)
			{
				if (diff == 0)
				{
					Mem.Copy(targetP, source, targetLength);
				}
				else
				{
					var decodedLength = LZ4Codec.Decode(
						source, sourceLength, targetP, targetLength);
					if (decodedLength != targetLength)
						throw new ArgumentException(
							$"Expected {targetLength} bytes but {decodedLength} has been decoded");
				}
			}

			return target;
		}
	}
}
