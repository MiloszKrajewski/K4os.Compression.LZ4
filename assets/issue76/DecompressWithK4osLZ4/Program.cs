using K4os.Compression.LZ4.Legacy;

var guid = "0A6D6D23-E750-41CC-AB4E-AAC7376FDF12";
var originalPath = "../../../../../../.corpus/mozilla";
var encodedPath = Path.Combine(Path.GetTempPath(), guid);
var decodedPath = Path.Combine(Path.GetTempPath(), guid + "_decoded");

using (var encoded = LZ4Legacy.Decode(File.OpenRead(encodedPath)))
using (var decoded = File.Create(decodedPath))
{
	encoded.CopyTo(decoded);
}

var originalBytes = File.ReadAllBytes(originalPath);
var decodedBytes = File.ReadAllBytes(decodedPath);

if (originalBytes.Length != decodedBytes.Length)
	throw new Exception("Lengths are different");

for (int i = 0; i < originalBytes.Length; i++)
	if (originalBytes[i] != decodedBytes[i])
		throw new Exception($"Bytes are different @ {i}");

Console.WriteLine($"{originalPath} and {encodedPath} are the same");
