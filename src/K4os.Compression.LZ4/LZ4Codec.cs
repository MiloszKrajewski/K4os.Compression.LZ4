using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using K4os.Compression.LZ4.Internal;

using LZ4EncoderContext = K4os.Compression.LZ4.Internal.LZ4_xx.LZ4_stream_t;

namespace K4os.Compression.LZ4
{
	public class LZ4Codec
	{
		public static int MaximumOutputSize(int length) => LZ4_xx.LZ4_compressBound(length);

		private static void Validate(byte[] buffer, int index, int length)
		{
			if (buffer == null) throw new ArgumentNullException(nameof(buffer), "cannot be null");

			var valid = index >= 0 && length >= 0 && index + length <= buffer.Length;
			if (!valid) throw new ArgumentException($"invald index/length combination: {index}/{length}");
		}

		public static unsafe int Encode(
			byte* source, byte* target,
			int sourceLength, int targetLength,
			LZ4Level level) =>
			level == LZ4Level.L00_FAST
				? LZ4_64.LZ4_compress_default(source, target, sourceLength, targetLength)
				: LZ4_64_HC.LZ4_compress_HC(source, target, sourceLength, targetLength, (int) level);

		public static unsafe int Encode(
			byte[] source, int sourceIndex, int sourceLength,
			byte[] target, int targetIndex, int targetLength,
			LZ4Level level)
		{
			Validate(source, sourceIndex, sourceLength);
			Validate(target, targetIndex, targetLength);
			fixed (byte* sourceP = source)
			fixed (byte* targetP = target)
				return Encode(sourceP + sourceIndex, targetP + targetIndex, sourceLength, targetLength, level);
		}

		public static byte[] Encode(byte[] source, int sourceIndex, int sourceLength, LZ4Level level)
		{
			Validate(source, sourceIndex, sourceLength);

			var bufferLength = MaximumOutputSize(sourceLength);
			var buffer = new byte[bufferLength];
			var targetLength = Encode(source, sourceIndex, sourceLength, buffer, 0, bufferLength, level);
			if (targetLength == bufferLength)
				return buffer;

			var target = new byte[targetLength];
			Buffer.BlockCopy(buffer, 0, target, 0, targetLength);
			return target;
		}

		public static unsafe int Decode(
			byte* source, byte* target, int sourceLength, int targetLength) =>
			LZ4_xx.LZ4_decompress_safe(source, target, sourceLength, targetLength);

		public static unsafe int Decode(
			byte[] source, int sourceIndex, int sourceLength,
			byte[] target, int targetIndex, int targetLength)
		{
			Validate(source, sourceIndex, sourceLength);
			Validate(target, targetIndex, targetLength);
			fixed (byte* sourceP = source)
			fixed (byte* targetP = target)
				return Decode(sourceP, targetP, sourceLength, target.Length);
		}

		public static byte[] Decode(byte[] source, int sourceIndex, int sourceLength, int targetLength)
		{
			Validate(source, sourceIndex, sourceLength);

			var result = new byte[targetLength];
			var decodedLength = Decode(source, sourceIndex, sourceLength, result, 0, targetLength);
			if (decodedLength != targetLength)
				throw new ArgumentException(
					$"Decoded length does not match expected value: {decodedLength}/{targetLength}");

			return result;
		}
	}

	public unsafe class LZ4Encoder: IDisposable
	{
		private LZ4EncoderContext* _state;
		private byte* _output0;
		private byte* _output1;

		public LZ4Encoder()
		{
			_state = (LZ4EncoderContext*) Mem.Alloc(sizeof(LZ4EncoderContext));
			_output0 = (byte*) Mem.Alloc(0x10000);
			_output1 = (byte*) Mem.Alloc(0x10000);
		}

		public IEnumerable<byte[]> Encode(byte[] source, int sourceIndex, int sourceLength)
		{

			return null;
		}

		protected virtual void DisposeManaged() { }

		protected virtual void DisposeUnmanaged()
		{
			if (_state != null) Mem.Free(_state);
			if (_output0 != null) Mem.Free(_output0);
			if (_output1 != null) Mem.Free(_output1);
			_state = null;
			_output0 = null;
			_output1 = null;
		}

		public void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		private void Dispose(bool disposing)
		{
			DisposeUnmanaged();
			if (disposing)
				DisposeManaged();
		}

		~LZ4Encoder()
		{
			Dispose(false);
		}
	}
}
