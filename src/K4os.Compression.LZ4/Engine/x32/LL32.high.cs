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
	#else
	using Mem = Internal.Mem64;
	#endif

	#if BIT32
	internal unsafe partial class LL32: LL
	#else
	internal unsafe partial class LL64: LL
	#endif
	{
		
	}
}

