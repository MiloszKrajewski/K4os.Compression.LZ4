using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("K4os.Compression.LZ4.Test")]
[assembly: InternalsVisibleTo("K4os.Compression.LZ4.Benchmarks")]

namespace K4os.Compression.LZ4
{
	internal class AssemblyHook
	{
		private AssemblyHook() { }
	}
}
