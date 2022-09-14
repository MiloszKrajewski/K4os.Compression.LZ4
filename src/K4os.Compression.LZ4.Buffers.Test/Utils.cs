using K4os.Compression.LZ4.Buffers.Test.Assets;
using Nerdbank.Streams;
using System.Buffers;
using System.IO.Pipelines;

namespace K4os.Compression.LZ4.Buffers.Test
{
    internal class Utils
    {
        public static byte[] GetAssetBytes(string name)
        {
            using var stream = typeof(AssetsPlaceholder).Assembly.GetManifestResourceStream(typeof(AssetsPlaceholder), name);
            if (stream is null)
            {
                throw new ArgumentException($"The asset {name} was not found");
            }

            var writer = new ArrayBufferWriter<byte>();
            while (true)
            {
                var buffer = writer.GetSpan();
                var count = stream.Read(buffer);
                if (count == 0)
                {
                    break;
                }

                writer.Advance(count);
            }

            return writer.WrittenSpan.ToArray();
        }

        public static async ValueTask WriteToAsync(PipeReader reader, IBufferWriter<byte> destination, CancellationToken cancellationToken = default)
        {
            while (true)
            {
                var result = await reader.ReadAsync(cancellationToken);
                foreach (var segment in result.Buffer)
                {
                    destination.Write(segment.Span);
                }
                reader.AdvanceTo(result.Buffer.End);
                if (result.IsCompleted)
                {
                    break;
                }
            }
        }

        public static ReadOnlySequence<byte> Fragmentize(ReadOnlySpan<byte> bytes)
        {
            var pool = new FakePool();
            var writer = new Sequence<byte>(pool) { MinimumSpanLength = 1 };

            foreach (var @byte in bytes)
            {
                var span = writer.GetSpan(1);
                span[0] = @byte;
                writer.Advance(1);
            }

            return writer.AsReadOnlySequence;
        }

        class FakePool : MemoryPool<byte>
        {
            public override int MaxBufferSize => throw new NotImplementedException();

            public override IMemoryOwner<byte> Rent(int minBufferSize = -1)
            {
                return new Lease(new byte[Math.Abs(minBufferSize)]);
            }

            protected override void Dispose(bool disposing)
            {
            }

            record class Lease(Memory<byte> Memory) : IMemoryOwner<byte>
            {
                public void Dispose()
                {
                }
            }
        }
    }
}
