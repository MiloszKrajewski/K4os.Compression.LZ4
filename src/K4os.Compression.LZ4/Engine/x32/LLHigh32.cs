//---------------------------------------------------------
//
// This file has been generated. All changes will be lost.
//
//---------------------------------------------------------
#define BIT32

// ReSharper disable IdentifierTypo
// ReSharper disable InconsistentNaming

namespace K4os.Compression.LZ4.Engine
{
	#if BIT32
	using Mem = Internal.Mem32;
	using LLFast = LLFast32;
	#else
	using Mem = Internal.Mem64;
	using LLFast = LLFast64;
	#endif

	#if BIT32
	internal class LLHigh32: LLHigh
	#else
	internal class LLHigh64: LLHigh
	#endif
	{
		
	}
}

