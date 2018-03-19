using System.Diagnostics;
using System.IO;

namespace K4os.Compression.LZ4.Streams.Test
{
	public class ReferenceLZ4
	{
		public static string Encode(string options, string input)
		{
			var executable = Tools.FindFile(".tools/lz4.exe");

			var output = Path.GetTempFileName();
			var startup = new ProcessStartInfo {
				FileName = executable,
				Arguments = $"{options} -f \"{input}\" \"{output}\"",
				CreateNoWindow = true,
				UseShellExecute = false,
				//WindowStyle = ProcessWindowStyle.Hidden
			};
			var process = Process.Start(startup);
			if (process == null)
				return null;

			process.WaitForExit();
			return output;
		}
	}
}
