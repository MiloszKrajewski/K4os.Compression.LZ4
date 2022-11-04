using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace K4os.Compression.LZ4.Streams.Internal;

/// <summary>
/// LZ4 stream essentials when wrapping another stream.
/// You most likely should not use it but it needs to be public as it is inherited from.
/// </summary>
public abstract class LZ4StreamOnStreamEssentials: LZ4StreamEssentials<Stream>
{
	private protected LZ4StreamOnStreamEssentials(Stream innerStream, bool leaveOpen):
		base(innerStream, leaveOpen) { }

	/// <inheritdoc />
	public override bool CanRead => InnerResource.CanRead;

	/// <inheritdoc />
	public override bool CanWrite => InnerResource.CanWrite;

	/// <inheritdoc />
	public override bool CanTimeout => InnerResource.CanTimeout;

	/// <inheritdoc />
	public override int ReadTimeout
	{
		get => InnerResource.ReadTimeout;
		set => InnerResource.ReadTimeout = value;
	}

	/// <inheritdoc />
	public override int WriteTimeout
	{
		get => InnerResource.WriteTimeout;
		set => InnerResource.WriteTimeout = value;
	}

	/// <inheritdoc />
	public override void Flush() =>
		InnerResource.Flush();

	/// <inheritdoc />
	public override Task FlushAsync(CancellationToken token) =>
		InnerResource.FlushAsync(token);
}
