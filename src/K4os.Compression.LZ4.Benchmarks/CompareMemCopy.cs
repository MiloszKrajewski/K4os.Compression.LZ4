using System;
using BenchmarkDotNet.Attributes;

namespace K4os.Compression.LZ4.Benchmarks
{
	public unsafe class CompareMemCopy
	{
		[Params(0x10, 0x100, 0x1000, 0x10000, 0x100000)]
		public int Size { get; set; }

		private byte* _source;
		private byte* _target;

		[GlobalSetup]
		public void Setup()
		{
			_source = (byte*) Mem.Alloc(Size);
			_target = (byte*) Mem.Alloc(Size);
		}

		[GlobalCleanup]
		public void Cleanup()
		{
			Mem.Free(_source);
			Mem.Free(_target);
		}

		[Benchmark]
		public void InlinedLoop()
		{
			var source = _source;
			var target = _target;
			var length = Size;

			while (length >= sizeof(ulong))
			{
				*(ulong*) target = *(ulong*) source;
				target += sizeof(ulong);
				source += sizeof(ulong);
				length -= sizeof(ulong);
			}

			if (length >= sizeof(uint))
			{
				*(uint*) target = *(uint*) source;
				target += sizeof(uint);
				source += sizeof(uint);
				length -= sizeof(uint);
			}

			if (length >= sizeof(ushort))
			{
				*(uint*) target = *(ushort*) source;
				target += sizeof(ushort);
				source += sizeof(ushort);
				length -= sizeof(ushort);
			}

			if (length > 0)
			{
				*target = *source;
				// target++; source++; length--;
			}
		}

		[Benchmark]
		public void Loop()
		{
			Mem.Copy(_target, _source, Size);
		}

		[Benchmark]
		public void WildLoop()
		{
			Mem.WildCopy(_target, _source, _target + Size);
		}

		[Benchmark]
		public void Builtin()
		{
			Buffer.MemoryCopy(_source, _target, Size, Size);
		}
	}
}
