using System;
using System.Linq;
using System.Text;
using Xunit;

namespace K4os.Compression.LZ4.Streams.Tests
{
	public class StreamInterfaceTests
	{
		public class TestStream: MemoryStream
		{
			public int Disposed { get; set; }

			protected override void Dispose(bool disposing)
			{
				Disposed++;
				base.Dispose(disposing);
			}
		}

		[Fact]
		public void WhenCloseIsCalledOnDecodeStreamThenDisposeIsTriggered()
		{
			var test = new TestStream();
			var lz4 = LZ4Stream.Decode(test);
			lz4.Close();

			Assert.Equal(1, test.Disposed);
		}

		[Fact]
		public void WhenCloseIsCalledOnEncodeStreamThenDisposeIsTriggered()
		{
			var test = new TestStream();
			var lz4 = LZ4Stream.Encode(test);
			lz4.Close();

			Assert.Equal(1, test.Disposed);
		}

		[Fact]
		public void WhenLeaveOpenIsTrueDisposeInNotCalled()
		{
			var test = new TestStream();
			var dec = LZ4Stream.Decode(test, leaveOpen: true);
			var enc = LZ4Stream.Encode(test, leaveOpen: true);
			enc.Close();
			dec.Close();

			Assert.Equal(0, test.Disposed);
		}

		[Fact]
		public void CallingDisposeMultipleTimesIsFine()
		{
			var test = new TestStream();
			var lz4 = LZ4Stream.Decode(test);
			lz4.Dispose();

			Assert.Equal(1, test.Disposed);

			for (var i = 0; i < 1000; i++)
				lz4.Dispose();

			// Dispose can be called more than once,
			// but did not crash (that's the important part)
			Assert.True(test.Disposed >= 1);
		}

		[Fact]
		public void Issue38()
		{
			var path = Path.GetTempFileName();
			var f = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.Read);
			var z = LZ4Stream.Encode(f);
			var w = new BinaryWriter(z, Encoding.UTF8, false);
			w.Write(Guid.NewGuid().ToString());
			w.Dispose();
			
			// Without Dispose (or when Dispose is not properly propagated) this causes exception:
			// System.IO.IOException: The process cannot access the file because it is being used by another process
			File.Delete(path);
		}
	}
}
