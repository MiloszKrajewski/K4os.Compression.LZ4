namespace K4os.Compression.LZ4.Engine;

/// <summary>Algorithm selection.</summary>
public enum Algorithm
{
	/// <summary>Intel and ARMv7 version of 32 bit algorithm.</summary>
	X32,

	/// <summary>Intel version of 64 bit algorithm.</summary>
	X64,
}