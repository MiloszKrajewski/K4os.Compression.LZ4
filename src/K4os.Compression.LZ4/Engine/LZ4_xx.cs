using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using K4os.Compression.LZ4.Internal;

// ReSharper disable IdentifierTypo
// ReSharper disable InconsistentNaming

namespace K4os.Compression.LZ4.Engine
{
	internal unsafe class LZ4_xx
	{
		// [StructLayout(LayoutKind.Sequential)]
		// [MethodImpl(MethodImplOptions.AggressiveInlining)]

		protected const int LZ4_MEMORY_USAGE = 14;
		protected const int LZ4_MAX_INPUT_SIZE = 0x7E000000;

		protected const int LZ4_HASHLOG = LZ4_MEMORY_USAGE - 2;
		protected const int LZ4_HASHTABLESIZE = 1 << LZ4_MEMORY_USAGE;
		protected const int LZ4_HASH_SIZE_U32 = 1 << LZ4_HASHLOG;

		protected const int ACCELERATION_DEFAULT = 1;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal static int LZ4_compressBound(int isize) =>
			isize > LZ4_MAX_INPUT_SIZE ? 0 : isize + isize / 255 + 16;

		[StructLayout(LayoutKind.Sequential)]
		internal struct LZ4_stream_t
		{
			public fixed uint hashTable[LZ4_HASH_SIZE_U32];
			public uint currentOffset;
			public uint initCheck;
			public byte* dictionary;
			// public byte* bufferStart; /* obsolete, used for slideInputBuffer */
			public uint dictSize;
		};

		[StructLayout(LayoutKind.Sequential)]
		internal struct LZ4_streamDecode_t
		{
			public byte* externalDict;
			public uint extDictSize;
			public byte* prefixEnd;
			public uint prefixSize;
		};

		protected const int MINMATCH = 4;

		protected const int WILDCOPYLENGTH = 8;
		protected const int LASTLITERALS = 5;
		protected const int MFLIMIT = WILDCOPYLENGTH + MINMATCH;
		protected const int LZ4_minLength = MFLIMIT + 1;

		protected const int KB = 1 << 10;
		protected const int MB = 1 << 20;
		protected const uint GB = 1u << 30;

		protected const int MAXD_LOG = 16;
		protected const int MAX_DISTANCE = (1 << MAXD_LOG) - 1;

		protected const int ML_BITS = 4;
		protected const uint ML_MASK = (1U << ML_BITS) - 1;
		protected const int RUN_BITS = 8 - ML_BITS;
		protected const uint RUN_MASK = (1U << RUN_BITS) - 1;

		protected const int LZ4_64Klimit = 64 * KB + (MFLIMIT - 1);
		protected const int LZ4_skipTrigger = 6;

		public enum limitedOutput_directive
		{
			noLimit = 0,
			limitedOutput = 1,
			limitedDestSize = 2,
		}

		public enum tableType_t
		{
			byPtr = 0,
			byU32 = 1,
			byU16 = 2
		}

		public enum dict_directive
		{
			noDict = 0,
			withPrefix64k,
			usingExtDict
		}

		public enum dictIssue_directive
		{
			noDictIssue = 0,
			dictSmall
		}

		public enum endCondition_directive
		{
			endOnOutputSize = 0,
			endOnInputSize = 1
		}

		public enum earlyEnd_directive
		{
			full = 0,
			partial = 1
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		protected static uint LZ4_hash4(uint sequence, tableType_t tableType)
		{
			var hashLog = tableType == tableType_t.byU16 ? LZ4_HASHLOG + 1 : LZ4_HASHLOG;
			return unchecked ((sequence * 2654435761u) >> (MINMATCH * 8 - hashLog));
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		protected static uint LZ4_hash5(ulong sequence, tableType_t tableType)
		{
			var hashLog = tableType == tableType_t.byU16 ? LZ4_HASHLOG + 1 : LZ4_HASHLOG;
			return unchecked ((uint) (((sequence << 24) * 889523592379ul) >> (64 - hashLog)));
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		protected static void LZ4_putPositionOnHash(
			byte* p, uint h, void* tableBase, tableType_t tableType, byte* srcBase)
		{
			switch (tableType)
			{
				case tableType_t.byPtr:
					((byte**) tableBase)[h] = p;
					return;
				case tableType_t.byU32:
					((uint*) tableBase)[h] = (uint) (p - srcBase);
					return;
				default:
					((ushort*) tableBase)[h] = (ushort) (p - srcBase);
					return;
			}
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		protected static byte* LZ4_getPositionOnHash(
			uint h, void* tableBase, tableType_t tableType, byte* srcBase)
		{
			switch (tableType)
			{
				case tableType_t.byPtr: return ((byte**) tableBase)[h];
				case tableType_t.byU32: return ((uint*) tableBase)[h] + srcBase;
				default: return ((ushort*) tableBase)[h] + srcBase;
			}
		}

		private static readonly uint[] inc32table = { 0, 1, 2, 1, 0, 4, 4, 4 };
		private static readonly int[] dec64table = { 0, 0, 0, -1, -4, 1, 2, 3 };

		public static int LZ4_decompress_generic(
			byte* src,
			byte* dst,
			int srcSize,
			int outputSize,
			endCondition_directive endOnInput,
			earlyEnd_directive partialDecoding,
			int targetOutputSize,
			dict_directive dict,
			byte* lowPrefix,
			byte* dictStart,
			int dictSize)
		{
			var ip = src;
			var iend = ip + srcSize;

			var op = dst;
			var oend = op + outputSize;
			var oexit = op + targetOutputSize;

			var dictEnd = dictStart + dictSize;

			var safeDecode = endOnInput == endCondition_directive.endOnInputSize;
			var checkOffset = safeDecode && dictSize < 64 * KB;

			if (partialDecoding != earlyEnd_directive.full && oexit > oend - MFLIMIT)
				oexit = oend - MFLIMIT;
			if (endOnInput == endCondition_directive.endOnInputSize && outputSize == 0)
				return srcSize == 1 && *ip == 0 ? 0 : -1;
			if (endOnInput != endCondition_directive.endOnInputSize && outputSize == 0)
				return *ip == 0 ? 1 : -1;

			for (;;)
			{
				int length;
				uint token = *ip++;

				if (ip + 14 + 2 <= iend
					&& op + 14 + 18 <= oend
					&& token < 15 << ML_BITS
					&& (token & ML_MASK) != 15)
				{
					var ll = (int) (token >> ML_BITS);
					int off = Mem.Peek16(ip + ll);
					var matchPtr = op + ll - off;
					if (off >= 18 && matchPtr >= lowPrefix)
					{
						var ml = (int) ((token & ML_MASK) + MINMATCH);
						Mem.Copy16(op, ip);
						op += ll;
						ip += ll + 2;
						Mem.Copy18(op, matchPtr);
						op += ml;
						continue;
					}
				}

				if ((length = (int) (token >> ML_BITS)) == RUN_MASK)
				{
					uint s;
					do
					{
						s = *ip++;
						length += (int) s;
					}
					while (
						(endOnInput != endCondition_directive.endOnInputSize || ip < iend - RUN_MASK)
						&& s == 255);

					if (safeDecode && op + length < op) goto _output_error;
					if (safeDecode && ip + length < ip) goto _output_error;
				}

				var cpy = op + length;
				if (endOnInput == endCondition_directive.endOnInputSize
					&& (
						cpy > (partialDecoding == earlyEnd_directive.partial ? oexit : oend - MFLIMIT)
						|| ip + length > iend - (2 + 1 + LASTLITERALS)
					)
					|| endOnInput != endCondition_directive.endOnInputSize && cpy > oend - WILDCOPYLENGTH)
				{
					if (partialDecoding == earlyEnd_directive.partial)
					{
						if (cpy > oend) goto _output_error;
						if (endOnInput == endCondition_directive.endOnInputSize && ip + length > iend)
							goto _output_error;
					}
					else
					{
						if (endOnInput != endCondition_directive.endOnInputSize && cpy != oend)
							goto _output_error;
						if (endOnInput == endCondition_directive.endOnInputSize
							&& (ip + length != iend || cpy > oend))
							goto _output_error;
					}

					Mem.Copy(op, ip, length);
					ip += length;
					op += length;
					break;
				}

				Mem.WildCopy(op, ip, cpy);
				ip += length;
				op = cpy;

				int offset = Mem.Peek16(ip);
				ip += 2;
				var match = op - offset;
				if (checkOffset && match + dictSize < lowPrefix)
					goto _output_error;

				Mem.Poke32(op, (uint) offset);

				length = (int) (token & ML_MASK);
				if (length == ML_MASK)
				{
					uint s;
					do
					{
						s = *ip++;
						if ((endOnInput == endCondition_directive.endOnInputSize) && (ip > iend - LASTLITERALS))
							goto _output_error;

						length += (int) s;
					}
					while (s == 255);

					if (safeDecode && op + length < op)
						goto _output_error;
				}

				length += MINMATCH;

				if (dict == dict_directive.usingExtDict && match < lowPrefix)
				{
					if (op + length > oend - LASTLITERALS) goto _output_error;

					if (length <= lowPrefix - match)
					{
						Mem.Move(op, dictEnd - (lowPrefix - match), length);
						op += length;
					}
					else
					{
						var copySize = (int) (lowPrefix - match);
						var restSize = length - copySize;
						Mem.Copy(op, dictEnd - copySize, copySize);
						op += copySize;
						if (restSize > (int) (op - lowPrefix))
						{
							var endOfMatch = op + restSize;
							var copyFrom = lowPrefix;
							while (op < endOfMatch) *op++ = *copyFrom++;
						}
						else
						{
							Mem.Copy(op, lowPrefix, restSize);
							op += restSize;
						}
					}

					continue;
				}

				cpy = op + length;
				if (offset < 8)
				{
					op[0] = match[0];
					op[1] = match[1];
					op[2] = match[2];
					op[3] = match[3];
					match += inc32table[offset];
					Mem.Copy(op + 4, match, 4);
					match -= dec64table[offset];
				}
				else
				{
					Mem.Copy8(op, match);
					match += 8;
				}

				op += 8;

				if (cpy > oend - 12)
				{
					var oCopyLimit = oend - (WILDCOPYLENGTH - 1);
					if (cpy > oend - LASTLITERALS)
						goto _output_error;

					if (op < oCopyLimit)
					{
						Mem.WildCopy(op, match, oCopyLimit);
						match += oCopyLimit - op;
						op = oCopyLimit;
					}

					while (op < cpy) *op++ = *match++;
				}
				else
				{
					Mem.Copy8(op, match);
					if (length > 16)
						Mem.WildCopy(op + 8, match + 8, cpy);
				}

				op = cpy; /* correction */
			}

			/* end of decoding */
			if (endOnInput == endCondition_directive.endOnInputSize)
				return (int) (op - dst); /* Nb of output bytes decoded */

			return (int) (ip - src); /* Nb of input bytes read */

			/* Overflow error detected */
			_output_error:
			return (int) -(ip - src) - 1;
		}

		public static int LZ4_decompress_safe(
			byte* source, byte* dest, int compressedSize, int maxDecompressedSize) =>
			LZ4_decompress_generic(
				source,
				dest,
				compressedSize,
				maxDecompressedSize,
				endCondition_directive.endOnInputSize,
				earlyEnd_directive.full,
				0,
				dict_directive.noDict,
				dest,
				null,
				0);

		public static int LZ4_decompress_safe_partial(
			byte* source, byte* dest, int compressedSize, int targetOutputSize, int maxDecompressedSize) =>
			LZ4_decompress_generic(
				source,
				dest,
				compressedSize,
				maxDecompressedSize,
				endCondition_directive.endOnInputSize,
				earlyEnd_directive.partial,
				targetOutputSize,
				dict_directive.noDict,
				dest,
				null,
				0);

		public static int LZ4_decompress_fast(byte* source, byte* dest, int originalSize) =>
			LZ4_decompress_generic(
				source,
				dest,
				0,
				originalSize,
				endCondition_directive.endOnOutputSize,
				earlyEnd_directive.full,
				0,
				dict_directive.withPrefix64k,
				dest - 64 * KB,
				null,
				64 * KB);

		public static int LZ4_decompress_safe_continue(
			LZ4_streamDecode_t* lz4sd, byte* source, byte* dest, int compressedSize,
			int maxOutputSize)
		{
			int result;

			if (lz4sd->prefixEnd == dest)
			{
				result = LZ4_decompress_generic(
					source,
					dest,
					compressedSize,
					maxOutputSize,
					endCondition_directive.endOnInputSize,
					earlyEnd_directive.full,
					0,
					dict_directive.usingExtDict,
					lz4sd->prefixEnd - lz4sd->prefixSize,
					lz4sd->externalDict,
					(int) lz4sd->extDictSize);
				if (result <= 0) return result;

				lz4sd->prefixSize += (uint) result;
				lz4sd->prefixEnd += result;
			}
			else
			{
				lz4sd->extDictSize = lz4sd->prefixSize;
				lz4sd->externalDict = lz4sd->prefixEnd - lz4sd->extDictSize;
				result = LZ4_decompress_generic(
					source,
					dest,
					compressedSize,
					maxOutputSize,
					endCondition_directive.endOnInputSize,
					earlyEnd_directive.full,
					0,
					dict_directive.usingExtDict,
					dest,
					lz4sd->externalDict,
					(int) lz4sd->extDictSize);
				if (result <= 0) return result;

				lz4sd->prefixSize = (uint) result;
				lz4sd->prefixEnd = dest + result;
			}

			return result;
		}

		public static int LZ4_decompress_fast_continue(
			LZ4_streamDecode_t* lz4sd, byte* source, byte* dest, int originalSize)
		{
			int result;

			if (lz4sd->prefixEnd == dest)
			{
				result = LZ4_decompress_generic(
					source,
					dest,
					0,
					originalSize,
					endCondition_directive.endOnOutputSize,
					earlyEnd_directive.full,
					0,
					dict_directive.usingExtDict,
					lz4sd->prefixEnd - lz4sd->prefixSize,
					lz4sd->externalDict,
					(int) lz4sd->extDictSize);
				if (result <= 0) return result;

				lz4sd->prefixSize += (uint) originalSize;
				lz4sd->prefixEnd += originalSize;
			}
			else
			{
				lz4sd->extDictSize = lz4sd->prefixSize;
				lz4sd->externalDict = lz4sd->prefixEnd - lz4sd->extDictSize;
				result = LZ4_decompress_generic(
					source,
					dest,
					0,
					originalSize,
					endCondition_directive.endOnOutputSize,
					earlyEnd_directive.full,
					0,
					dict_directive.usingExtDict,
					dest,
					lz4sd->externalDict,
					(int) lz4sd->extDictSize);
				if (result <= 0) return result;

				lz4sd->prefixSize = (uint) originalSize;
				lz4sd->prefixEnd = dest + originalSize;
			}

			return result;
		}

		public static int LZ4_decompress_usingDict_generic(
			byte* source, byte* dest, int compressedSize, int maxOutputSize, int safe, byte* dictStart,
			int dictSize)
		{
			if (dictSize == 0)
				return LZ4_decompress_generic(
					source,
					dest,
					compressedSize,
					maxOutputSize,
					(endCondition_directive) safe,
					earlyEnd_directive.full,
					0,
					dict_directive.noDict,
					dest,
					null,
					0);

			if (dictStart + dictSize != dest)
				return LZ4_decompress_generic(
					source,
					dest,
					compressedSize,
					maxOutputSize,
					(endCondition_directive) safe,
					earlyEnd_directive.full,
					0,
					dict_directive.usingExtDict,
					dest,
					dictStart,
					dictSize);

			if (dictSize >= 64 * KB - 1)
				return LZ4_decompress_generic(
					source,
					dest,
					compressedSize,
					maxOutputSize,
					(endCondition_directive) safe,
					earlyEnd_directive.full,
					0,
					dict_directive.withPrefix64k,
					dest - 64 * KB,
					null,
					0);

			return LZ4_decompress_generic(
				source,
				dest,
				compressedSize,
				maxOutputSize,
				(endCondition_directive) safe,
				earlyEnd_directive.full,
				0,
				dict_directive.noDict,
				dest - dictSize,
				null,
				0);
		}

		public static int LZ4_decompress_safe_usingDict(
			byte* source, byte* dest, int compressedSize, int maxOutputSize, byte* dictStart,
			int dictSize) =>
			LZ4_decompress_usingDict_generic(
				source,
				dest,
				compressedSize,
				maxOutputSize,
				1,
				dictStart,
				dictSize);

		public static int LZ4_decompress_fast_usingDict(
			byte* source, byte* dest, int originalSize, byte* dictStart, int dictSize) =>
			LZ4_decompress_usingDict_generic(source, dest, 0, originalSize, 0, dictStart, dictSize);

		public static int LZ4_decompress_safe_forceExtDict(
			byte* source, byte* dest, int compressedSize, int maxOutputSize, byte* dictStart,
			int dictSize) =>
			LZ4_decompress_generic(
				source,
				dest,
				compressedSize,
				maxOutputSize,
				endCondition_directive.endOnInputSize,
				earlyEnd_directive.full,
				0,
				dict_directive.usingExtDict,
				dest,
				dictStart,
				dictSize);

		public static void LZ4_renormDictT(LZ4_stream_t* dict, byte* src)
		{
			if (dict->currentOffset <= 0x80000000 && dict->currentOffset <= (ulong) src)
				return;

			var delta = dict->currentOffset - 64 * KB;
			var dictEnd = dict->dictionary + dict->dictSize;
			for (var i = 0; i < LZ4_HASH_SIZE_U32; i++)
			{
				if (dict->hashTable[i] < delta)
					dict->hashTable[i] = 0;
				else
					dict->hashTable[i] -= delta;
			}

			dict->currentOffset = 64 * KB;
			if (dict->dictSize > 64 * KB) dict->dictSize = 64 * KB;
			dict->dictionary = dictEnd - dict->dictSize;
		}

		public static int LZ4_saveDict(LZ4_stream_t* dict, byte* safeBuffer, int dictSize)
		{
			var previousDictEnd = dict->dictionary + dict->dictSize;

			if ((uint) dictSize > 64 * KB) dictSize = 64 * KB;
			if ((uint) dictSize > dict->dictSize) dictSize = (int) dict->dictSize;

			Mem.Move(safeBuffer, previousDictEnd - dictSize, dictSize);

			dict->dictionary = safeBuffer;
			dict->dictSize = (uint) dictSize;

			return dictSize;
		}

		public static void LZ4_setStreamDecode(
			LZ4_streamDecode_t* lz4sd, byte* dictionary, int dictSize)
		{
			lz4sd->prefixSize = (uint) dictSize;
			lz4sd->prefixEnd = dictionary + dictSize;
			lz4sd->externalDict = null;
			lz4sd->extDictSize = 0;
		}
	}
}
