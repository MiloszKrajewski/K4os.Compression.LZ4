using System;
using System.IO;

namespace TestHelpers;

public class TempFile: IDisposable
{
	public static TempFile Create() => new();
	public string FileName { get; } = Path.GetTempFileName();
	public void Dispose() => File.Delete(FileName);
}
