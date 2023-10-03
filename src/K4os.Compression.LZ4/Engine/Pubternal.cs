#nullable enable

using K4os.Compression.LZ4.Internal;

namespace K4os.Compression.LZ4.Engine;

/// <summary>
/// Existence of this class is an admission of failure.
/// I failed to export internals to test assemblies.
/// Using InternalsVisibleTo work, of course, but with signing (which was requested
/// in https://github.com/MiloszKrajewski/K4os.Compression.LZ4/issues/9) it is
/// absolute PITA. So no, I'm not using InternalsVisibleTo I will just expose this
/// little class with some "redirects" to real internals. 
/// </summary>
public static unsafe class Pubternal
{
	/// <summary>Pubternal wrapper for LZ4_stream_t.</summary>
	public class FastContext: UnmanagedResources
	{
		internal LL.LZ4_stream_t* Context { get; }

		/// <summary>Creates new instance of wrapper for LZ4_stream_t.</summary>
		public FastContext() =>
			Context = (LL.LZ4_stream_t*) Mem.AllocZero(sizeof(LL.LZ4_stream_t));

		/// <inheritdoc/>
		protected override void ReleaseUnmanaged() => Mem.Free(Context);
	}

	/// <summary>
	/// Compresses chunk of data using LZ4_compress_fast_continue.
	/// </summary>
	/// <param name="context">Wrapper for LZ4_stream_t</param>
	/// <param name="source">Source block address.</param>
	/// <param name="target">Target block address.</param>
	/// <param name="sourceLength">Source block length.</param>
	/// <param name="targetLength">Target block length.</param>
	/// <param name="acceleration">Acceleration.</param>
	/// <returns>Number of bytes actually written to target.</returns>
	public static int CompressFast(
		FastContext context,
		byte* source, byte* target,
		int sourceLength, int targetLength,
		int acceleration) =>
		LLxx.LZ4_compress_fast_continue(
			context.Context,
			source, target,
			sourceLength, targetLength,
			acceleration);
}
