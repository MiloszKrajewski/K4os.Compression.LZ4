## 1.1.7 (2019/05/13)
* issues 18 & 22: returning 0 bytes on EoF many times (not just once)

## 1.1.5 (2019/05/12)
* Added explicit "unchecked" around hash calculation

## 1.1.4 (2019/04/29)
* Moved build process to FAKE 5 (no functionality added)

## 1.1.3 (2019/04/28)
* Added lz4net compatible pickler

## 1.1.2 (2019/04/28)
* Added lz4net compatible stream

## 1.1.1 (2018/11/06)
* Position and Length for LZ4EncoderStream

## 1.1.0 (2018/11/04)
* Signed assemblies
* Independent block encoder and decoder (performance)
* Better XML doc
* Breaking changes to pubternals

## 1.0.3 (2018/10/12)
* added auto-download of nuget (Windows only)
* merged fix for slow streams (https://github.com/MiloszKrajewski/K4os.Compression.LZ4/pull/8)
* Dictionary is back (although, it is still ignored)

## 1.0.2 (2018/10/03)
* updated package information
* added Position to Decode stream
* added Length to Decode stream, if known

## 1.0.0-beta (2018/09/09)
* based on lz4 1.8.1
* fully working and tested, but some features are missing