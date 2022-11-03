using LZ4;

var guid = "0A6D6D23-E750-41CC-AB4E-AAC7376FDF12";
var originalPath = "../../../../../../.corpus/mozilla";
var encodedPath = Path.Combine(Path.GetTempPath(), guid);

using (var original = File.OpenRead(originalPath))
using (var encoded = new LZ4Stream(File.Create(encodedPath), LZ4StreamMode.Compress))
{
	original.CopyTo(encoded);
}

Console.WriteLine($"{encodedPath} created");