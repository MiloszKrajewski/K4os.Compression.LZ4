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
* aoved build process to FAKE 5 (no functionality added)

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
