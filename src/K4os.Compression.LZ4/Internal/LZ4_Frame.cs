using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using LZ4F_errorCode_t = System.UIntPtr;
using size_t = System.UInt64;

namespace K4os.Compression.LZ4.Internal
{
	internal unsafe class LZ4_Frame: LZ4_xx
	{
		enum LZ4F_blockSizeID_t
		{
			LZ4F_default = 0,
			LZ4F_max64KB = 4,
			LZ4F_max256KB = 5,
			LZ4F_max1MB = 6,
			LZ4F_max4MB = 7
		}

		enum LZ4F_blockMode_t
		{
			LZ4F_blockLinked = 0,
			LZ4F_blockIndependent
		}

		enum LZ4F_contentChecksum_t
		{
			LZ4F_noContentChecksum = 0,
			LZ4F_contentChecksumEnabled
		}

		enum LZ4F_blockChecksum_t
		{
			LZ4F_noBlockChecksum = 0,
			LZ4F_blockChecksumEnabled
		}

		enum LZ4F_frameType_t
		{
			LZ4F_frame = 0,
			LZ4F_skippableFrame
		}

		/* LZ4F_frameInfo_t :
		 * makes it possible to set or read frame parameters.
		 * It's not required to set all fields, as long as the structure was initially memset() to zero.
		 * For all fields, 0 sets it to default value */
		[StructLayout(LayoutKind.Sequential)]
		struct LZ4F_frameInfo_t
		{
			public LZ4F_blockSizeID_t blockSizeID; // max64KB, max256KB, max1MB, max4MB ; 0 == default
			public LZ4F_blockMode_t blockMode; // LZ4F_blockLinked, LZ4F_blockIndependent ; 0 == default

			// if enabled, frame is terminated with a 32-bits checksum of decompressed data ; 0 == disabled (default)
			public LZ4F_contentChecksum_t contentChecksumFlag;

			public LZ4F_frameType_t frameType; // read-only field : LZ4F_frame or LZ4F_skippableFrame
			public ulong contentSize; // Size of uncompressed content ; 0 == unknown

			// Dictionary ID, sent by the compressor to help decoder select the correct dictionary; 0 == no dictID provided
			public uint dictID;

			// if enabled, each block is followed by a checksum of block's compressed data ; 0 == disabled (default)
			public LZ4F_blockChecksum_t blockChecksumFlag;
		}

		/* LZ4F_preferences_t :
		 * makes it possible to supply detailed compression parameters to the stream interface.
		 * It's not required to set all fields, as long as the structure was initially memset() to zero.
		 * All reserved fields must be set to zero. */
		[StructLayout(LayoutKind.Sequential)]
		struct LZ4F_preferences_t
		{
			public LZ4F_frameInfo_t frameInfo;

			// 0 == default (fast mode); values above LZ4HC_CLEVEL_MAX count as LZ4HC_CLEVEL_MAX; values below 0 trigger "fast acceleration", proportional to value
			public int compressionLevel;

			// 1 == always flush, to reduce usage of internal buffers
			public uint autoFlush;
			// public fixed uint reserved[4]; // must be zero for forward compatibility
		}

		// struct LZ4F_cctx_s LZ4F_cctx;   /* incomplete type */
		// LZ4F_cctx* LZ4F_compressionContext_t;   /* for compatibility with previous API version */

		[StructLayout(LayoutKind.Sequential)]
		struct LZ4F_compressOptions_t
		{
			// 1 == src content will remain present on future calls to LZ4F_compress(); skip copying src content within tmp buffer
			uint stableSrc;
			// uint reserved[3];
		}

		const int LZ4F_VERSION = 100;
		const int LZ4F_HEADER_SIZE_MAX = 19;

		[StructLayout(LayoutKind.Sequential)]
		struct LZ4F_decompressOptions_t
		{
			// pledge that at least 64KB+64Bytes of previously decompressed data remain unmodifed where it was decoded. This optimization skips storage operations in tmp buffers
			uint stableDst;
			// uint reserved[3];  /* must be set to zero for forward compatibility */
		}

		[StructLayout(LayoutKind.Sequential)]
		struct LZ4F_CDict
		{
			void* dictContent;
			LZ4_xx.LZ4_stream_t* fastCtx;
			LZ4_64_HC.LZ4HC_CCtx_t* HCCtx;
		}

		const uint _1BIT = 0x01;
		const uint _2BITS = 0x03;
		const uint _3BITS = 0x07;
		const uint _4BITS = 0x0F;
		const uint _8BITS = 0xFF;

		const uint LZ4F_MAGIC_SKIPPABLE_START = 0x184D2A50U;
		const uint LZ4F_MAGICNUMBER = 0x184D2204U;
		const uint LZ4F_BLOCKUNCOMPRESSED_FLAG = 0x80000000U;
		const LZ4F_blockSizeID_t LZ4F_BLOCKSIZEID_DEFAULT = LZ4F_blockSizeID_t.LZ4F_max64KB;

		const uint minFHSize = 7;
		const uint maxFHSize = LZ4F_HEADER_SIZE_MAX; /* 19 */
		const uint BHSize = 4;

		struct XXH32_state_t { }

		private static uint XXH32(void* data, size_t length, uint seed) => seed;

		struct LZ4F_cctx_t
		{
			LZ4F_preferences_t prefs;
			uint version;
			uint cStage;
			LZ4F_CDict* cdict;
			size_t maxBlockSize;
			size_t maxBufferSize;
			byte* tmpBuff;
			byte* tmpIn;
			size_t tmpInSize;
			ulong totalInSize;
			XXH32_state_t xxh;
			void* lz4CtxPtr;
			uint lz4CtxLevel; // 0: unallocated;  1: LZ4_stream_t;  3: LZ4_streamHC_t
		}

		private static Exception LZ4F_ERROR_maxBlockSize_invalid() => new Exception();

		static size_t LZ4F_getBlockSize(LZ4F_blockSizeID_t blockSizeID)
		{
			switch (blockSizeID)
			{
				case LZ4F_blockSizeID_t.LZ4F_max64KB: return 64 * KB;
				case LZ4F_blockSizeID_t.LZ4F_max256KB: return 256 * KB;
				case LZ4F_blockSizeID_t.LZ4F_max1MB: return 1 * MB;
				case LZ4F_blockSizeID_t.LZ4F_max4MB: return 4 * MB;
				default: return LZ4F_getBlockSize(LZ4F_BLOCKSIZEID_DEFAULT);
			}
		}

		static byte LZ4F_headerChecksum(void* header, size_t length) =>
			(byte) (XXH32(header, length, 0) >> 8);

		static LZ4F_blockSizeID_t LZ4F_optimalBSID(
			LZ4F_blockSizeID_t requestedBSID, size_t srcSize)
		{
			LZ4F_blockSizeID_t proposedBSID = LZ4F_blockSizeID_t.LZ4F_max64KB;
			size_t maxBlockSize = 64 * KB;
			while (requestedBSID > proposedBSID)
			{
				if (srcSize <= maxBlockSize)
					return proposedBSID;

				proposedBSID = (LZ4F_blockSizeID_t) ((int) proposedBSID + 1);
				maxBlockSize <<= 2;
			}

			return requestedBSID;
		}

		/* LZ4F_compressBound_internal() :
		 * Provides dstCapacity given a srcSize to guarantee operation success in worst case situations.
		 * prefsPtr is optional : if NULL is provided, preferences will be set to cover worst case scenario.
		 * @return is always the same for a srcSize and prefsPtr, so it can be relied upon to size reusable buffers.
		 * When srcSize==0, LZ4F_compressBound() provides an upper bound for LZ4F_flush() and LZ4F_compressEnd() operations.
		 */
		static size_t LZ4F_compressBound_internal(
			size_t srcSize,
			LZ4F_preferences_t* preferencesPtr,
			size_t alreadyBuffered)
		{
			LZ4F_preferences_t prefsNull;
			Mem.Zero((byte*) &prefsNull, sizeof(LZ4F_preferences_t));
			prefsNull.frameInfo.contentChecksumFlag =
				LZ4F_contentChecksum_t.LZ4F_contentChecksumEnabled; // worst case
			{
				var prefsPtr = preferencesPtr == null ? &prefsNull : preferencesPtr;
				var flush = (prefsPtr->autoFlush != 0) || (srcSize == 0);
				var blockID = prefsPtr->frameInfo.blockSizeID;
				var blockSize = LZ4F_getBlockSize(blockID);
				var maxBuffered = blockSize - 1;
				var bufferedSize = Math.Min(alreadyBuffered, maxBuffered);
				var maxSrcSize = srcSize + bufferedSize;
				var nbFullBlocks = (uint) (maxSrcSize / blockSize);
				var partialBlockSize = maxSrcSize & (blockSize - 1);
				var lastBlockSize = flush ? partialBlockSize : 0;
				var nbBlocks = nbFullBlocks + (lastBlockSize > 0 ? 1u : 0);

				size_t blockHeaderSize = 4;
				size_t blockCRCSize = 4 * (uint) prefsPtr->frameInfo.blockChecksumFlag;
				size_t frameEnd = 4 * (uint) prefsPtr->frameInfo.contentChecksumFlag + 4;

				return
					(blockHeaderSize + blockCRCSize) * nbBlocks
					+ blockSize * nbFullBlocks
					+ lastBlockSize
					+ frameEnd;
			}
		}
	}
}
