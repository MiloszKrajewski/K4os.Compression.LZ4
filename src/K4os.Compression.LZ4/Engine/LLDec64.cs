// ReSharper disable IdentifierTypo
// ReSharper disable InconsistentNaming
// ReSharper disable AccessToStaticMemberViaDerivedType
// ReSharper disable ConditionIsAlwaysTrueOrFalse
// ReSharper disable BuiltInTypeReferenceStyle

using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace K4os.Compression.LZ4.Engine
{
	#if BIT32
	using Mem = Internal.Mem32;
	using ptr_t = Int32;
	using size_t = Int32;
	#else
	using Mem = Internal.Mem64;
	using ptr_t = Int64;
	using size_t = Int32;

	#endif

	#if BIT32
	internal unsafe class LLDec32: LLTools32
	#else
	internal unsafe class LLDec64: LLTools64
	#endif
	{ }
}