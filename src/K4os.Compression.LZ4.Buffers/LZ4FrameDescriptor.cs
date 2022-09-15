using K4os.Compression.LZ4.Internal;
using K4os.Hash.xxHash;
using System.Buffers.Binary;
using System.Diagnostics.CodeAnalysis;

namespace K4os.Compression.LZ4.Buffers
{
    public sealed class LZ4FrameDescriptor
    {
        public const int MaxSize = 15;

        public const int Version = 0b01;
        public bool BlockIndependenceFlag { get; set; }
        public bool BlockChecksumFlag { get; set; }
        public bool ContentChecksumFlag { get; set; }
        public int BlockMaximumSize { get; set; } = Mem.K64;
        public long? ContentSize { get; set; }
        public int? DictionaryId { get; set; }

        /// <summary>
        /// The default frame descriptor values matching the defaults from the lz4 cli.
        /// </summary>
        public static readonly LZ4FrameDescriptor Default = new()
        {
            BlockIndependenceFlag = false,
            BlockChecksumFlag = false,
            ContentChecksumFlag = true,
            BlockMaximumSize = Mem.M4,
            ContentSize = null,
            DictionaryId = null,
        };

        internal static bool TryRead(ReadOnlySpan<byte> source, [MaybeNullWhen(false)] out LZ4FrameDescriptor result, out int length)
        {
            length = 0;
            if (source.Length < 2)
            {
                result = null;
                return false;
            }

            var FLG = source[length++];
            var BD = source[length++];
            var version = (FLG >> 6) & 0x03;
            var blockIndependeneFlag = (FLG & (1 << 5)) > 0;
            var blockChecksumFlag = (FLG & (1 << 4)) > 0;
            var hasContentSize = (FLG & (1 << 3)) > 0;
            var contentChecksumFlag = (FLG & (1 << 2)) > 0;
            var hasDictionaryId = (FLG & (1 << 0)) > 0;

            if (version != Version)
            {
                throw new InvalidDataException("Invalid LZ4 frame version");
            }

            var contentSize = 0UL;
            var dictionaryId = 0U;

            if (hasContentSize)
            {
                if (!BinaryPrimitives.TryReadUInt64LittleEndian(source[length..], out contentSize))
                {
                    result = null;
                    return false;
                }

                length += 8;
            }

            if (hasDictionaryId)
            {
                if (!BinaryPrimitives.TryReadUInt32LittleEndian(source[length..], out dictionaryId))
                {
                    result = null;
                    return false;
                }

                length += 4;
            }

            if (source.Length < length + 1)
            {
                result = null;
                return false;
            }

            var digest = (byte)(XXH32.DigestOf(source[..length]) >> 8);
            var HC = source[length++];

            if (HC != digest)
            {
                throw new InvalidDataException($"The header digest {digest} did not match the header checksum field {HC}");
            }

            result = new LZ4FrameDescriptor()
            {
                BlockIndependenceFlag = blockIndependeneFlag,
                BlockChecksumFlag = blockChecksumFlag,
                ContentSize = hasContentSize ? (long)contentSize : null,
                ContentChecksumFlag = contentChecksumFlag,
                DictionaryId = hasDictionaryId ? (int)dictionaryId : null,
                BlockMaximumSize = GetBlockMaximumSize((BD >> 4) & 0x07),
            };
            return true;
        }

        internal int Write(Span<byte> destination)
        {
            var length = 0;

            var FLG = Version << 6;
            if (BlockIndependenceFlag) FLG |= 1 << 5;
            if (BlockChecksumFlag) FLG |= 1 << 4;
            if (ContentSize.HasValue) FLG |= 1 << 3;
            if (ContentChecksumFlag) FLG |= 1 << 2;
            if (DictionaryId.HasValue) FLG |= 1 << 0;

            destination[length++] = (byte)FLG;

            // BD byte
            var BD = GetBlockMaximumSizeTableValue(BlockMaximumSize) << 4;
            destination[length++] = (byte)BD;

            // Content size (optional)
            if (ContentSize.HasValue)
            {
                BinaryPrimitives.WriteUInt64LittleEndian(destination[length..], (ulong)ContentSize.Value);
                length += 8;
            }

            // Dictionary id (optional)
            if (DictionaryId.HasValue)
            {
                BinaryPrimitives.WriteUInt32LittleEndian(destination[length..], (uint)DictionaryId.Value);
                length += 4;
            }

            // Header checksum
            destination[length] = (byte)(XXH32.DigestOf(destination[..length]) >> 8);
            length++;

            return length;
        }

        private static int GetBlockMaximumSizeTableValue(int blockSize) =>
            blockSize <= Mem.K64 ? 4 :
            blockSize <= Mem.K256 ? 5 :
            blockSize <= Mem.M1 ? 6 :
            blockSize <= Mem.M4 ? 7 :
            throw new ArgumentException("Invalid block size");

        private static int GetBlockMaximumSize(int tableValue) => tableValue switch
        {
            4 => Mem.K64,
            5 => Mem.K256,
            6 => Mem.M1,
            7 => Mem.M4,
            _ => throw new InvalidDataException("Invalid maximum block size")
        };
    }
}
