## 1.3.8 (2023/03/17)
* FIXED #96: fixed issue returning wrong compressed length when compressing into Span<byte> 

## 1.3.7-beta (2023/10/03)
* ADDED #61: partial decompression support (see #61, #90) (thanks: Sewer56)
* CHANGED: build process from FAKE (sorry!) to Nuke

## 1.3.6 (2023/08/15)
* FIXED #85: removed call to Dispose from DisposeAsync in LZ4EncoderStream

## 1.3.5 (2023/01/06)
* NOTE: just a release without beta tag

## 1.3.4-beta (2022/11/09)
* ADDED: block and content checksum support

## 1.3.3-beta (2022/11/06)
* FIXED: (hopefully) System.Runtime.CompilerServices.Unsafe versioning issues

## 1.3.2-beta (2022/11/05)
* ADDED: retrofitted support for Pipes for older .NET versions
* ADDED: enabled hardware CTZ for .NET 5 and above (it was .NET 5 only before)

## 1.3.0-beta (2022/11/03)
* CHANGED: Dropped compatibility with .NET 4.5 and .NET Standard 1.6
* ADDED: Added .NET 6.0 as a target
* ADDED: FrameEncoder / FrameDecoder are now abstraction of stream
* ADDED: Streaming (LZ4 Frame) support for Pipes (NET 5.0+)
* ADDED #6: Streaming (LZ4 Frame) support for Span and Memory
* ADDED #14: Streaming (LZ4 Frame) support for ReadOnlySequence, BufferWriter
* FIXED #72: Added (again) System.Runtime.CompilerServices.Unsafe as dependency for .NET 4.6, .NET Standard 2.0, .NET Standard 2.1
* FIXED #69: Added Pickle/Unpickle overload taking BufferWriter as generic argument not interface (potential performance improvement)
* NOTE: I'm still unhappy with API, but it cannot be addressed without breaking changes, so while this version may receive some bugfixes, there is an overhaul on the horizon

## 1.2.17 (2021/12/07)
* ADDED: Forced K4os.Hash.xxHash 1.0.7

## 1.2.16 (2021/12/01)
* REMOVED: System.Memory as dependency for .NET Standard 2.1 and .NET 5

## 1.2.15 (2021/10/22)
* ADDED #64: block decompression with external dictionary
* FIXED: Minor fixed to unit tests

## 1.2.13 (2021/10/12)
* REMOVED: reference to System.Runtime.CompilerServices.Unsafe (which is transitive)
* ADDED: explicit release for netstandard 2.1

## 1.2.12 (2021/08/01)
* ADDED: some testing with Tar*Stream from SharpZipLib

## 1.2.10-beta (2021/01/29)
* ADDED #14 (partial): support for IBufferWriter in LZ4Pickler

## 1.2.8-beta (2020/09/26)
* FIXED: Implicit reference to System.Runtime.CompilerServices.Unsafe

## 1.2.7 (2020/09/13)
* FIXED: LZ4Pickler is now memory alignment agnostic

## 1.2.6 (2020/09/12)
* FIXED #41: fixed nasty async reader bug (async vs struct)

## 1.2.5 (2020/09/04)
* ADDED #34: true async support for read and write
* ADDED #35: full async support for .NET Standard 2.1
* FIXED #38: stream is now properly disposed on Close()
* ADDED #40: unaligned memory access moved to 32-bit

## 1.2.4-alpha (2020/08/02)
* unaligned memory access methods to address ARMv7/Unity bug   

## 1.2.2-beta (2020/05/13)
* issue 32 (slow when combined with CryptStream) fixed 
* breaking change: interactive mode is no longer default  

## 1.2.1-beta (2020/03/15)
* port of lz4 1.9.2
* explicit support for both 32 and 64 environments

## 1.1.11 (2019/07/03)
* added (experimental!) support for .NET 4.5

## 1.1.10 (2019/06/11)
* added explicit "unchecked" around De Bruijn calculation

## 1.1.9 (2019/05/14)
* issue 22 returns: fix for ReadByte/WriteByte bug

## 1.1.7 (2019/05/13)
* issues 18 & 22: returning 0 bytes on EoF many times (not just once)

## 1.1.5 (2019/05/12)
* added explicit "unchecked" around hash calculation

## 1.1.4 (2019/04/29)
* moved build process to FAKE 5 (no functionality added)

## 1.1.3 (2019/04/28)
* added lz4net compatible pickler

## 1.1.2 (2019/04/28)
* added lz4net compatible stream

## 1.1.1 (2018/11/06)
* position and Length for LZ4EncoderStream

## 1.1.0 (2018/11/04)
* signed assemblies
* independent block encoder and decoder (performance)
* better XML doc
* breaking changes to pubternals

## 1.0.3 (2018/10/12)
* added auto-download of nuget (Windows only)
* merged fix for slow streams (https://github.com/MiloszKrajewski/K4os.Compression.LZ4/pull/8)
* dictionary is back (although, it is still ignored)

## 1.0.2 (2018/10/03)
* updated package information
* added Position to Decode stream
* added Length to Decode stream, if known

## 1.0.0-beta (2018/09/09)
* based on lz4 1.8.1
* fully working and tested, but some features are missing
