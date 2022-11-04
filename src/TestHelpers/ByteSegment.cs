using System;
using System.Buffers;
using System.Collections.Generic;

namespace TestHelpers;

public class ByteSegment: ReadOnlySequenceSegment<byte>
{
	public static ReadOnlySequence<byte> BuildSequence(
		ReadOnlyMemory<byte> memory, Func<int> sizer)
	{
		var offset = 0;
		var length = memory.Length;
		var blocks = new List<ReadOnlyMemory<byte>>();

		while (length > 0)
		{
			var size = Math.Min(Math.Max(sizer(), 1), length);
			blocks.Add(memory.Slice(offset, size));
			offset += size;
			length -= size;
		}

		return BuildSequence(blocks);
	}

	public static ReadOnlySequence<byte> BuildSequence(
		IEnumerable<ReadOnlyMemory<byte>> segments)
	{
		var first = default(ByteSegment);
		var current = default(ByteSegment);

		foreach (var segment in segments)
		{
			current = current is null
				? first = new ByteSegment(segment)
				: current.Append(segment);
		}

		return current is null
			? ReadOnlySequence<byte>.Empty
			: new ReadOnlySequence<byte>(first, 0, current, current.Memory.Length);
	}

	public ByteSegment(ReadOnlyMemory<byte> memory) { Memory = memory; }

	public ByteSegment Append(ReadOnlyMemory<byte> memory)
	{
		var segment = new ByteSegment(memory) { RunningIndex = RunningIndex + Memory.Length };
		Next = segment;
		return segment;
	}

	public ReadOnlySequenceSegment<byte> Last => GetLast(this);

	private static ReadOnlySequenceSegment<byte> GetLast(ReadOnlySequenceSegment<byte> current) =>
		current.Next switch { null => null, var next => GetLast(next) };
}
