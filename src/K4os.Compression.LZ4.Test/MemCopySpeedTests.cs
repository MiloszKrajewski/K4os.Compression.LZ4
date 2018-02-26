using System;
using System.Runtime.CompilerServices;
using Xunit;
using Xunit.Abstractions;

namespace K4os.Compression.LZ4.Test
{
	public unsafe class MemCopySpeedTests: TestBase
	{
		public MemCopySpeedTests(ITestOutputHelper output): base(output) { }

		#region SUT

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static void CopyX(byte* target, byte* source, int length) =>
			Buffer.MemoryCopy(source, target, length, length);

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static void MoveX(byte* target, byte* source, int length) =>
			Buffer.MemoryCopy(source, target, length, length);

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static void WildCopyX(byte* target, byte* source, void* limit)
		{
			var length = (byte*) limit - target;
			Buffer.MemoryCopy(source, target, length, length);
		}

		#endregion

		[Theory]
		[InlineData(0x400, 10000000)]
		[InlineData(0x10000, 100000)]
		[InlineData(0x100000, 10000)]
		public void TestMemCopy(int length, int repeat)
		{
			var source = (byte*) Mem.Alloc(length + 8);
			var target = (byte*) Mem.Alloc(length + 8);
			Lorem.Fill(source, length);

			Measure($"MemCopy({length}).LZ4", repeat, () => Mem.Copy(target, source, length));
			Measure($"MemCopy({length}).NET", repeat, () => CopyX(target, source, length));
		}

		[Theory]
		[InlineData(0x400, 10000000)]
		[InlineData(0x10000, 100000)]
		[InlineData(0x100000, 10000)]
		public void TestWildCopy(int length, int repeat)
		{
			var source = (byte*) Mem.Alloc(length + 8);
			var target = (byte*) Mem.Alloc(length + 8);
			Lorem.Fill(source, length);

			Measure($"WildCopy({length}).LZ4", repeat, () => Mem.WildCopy(target, source, target + length));
			Measure($"WildCopy({length}).NET", repeat, () => WildCopyX(target, source, target + length));
		}

	}
}
