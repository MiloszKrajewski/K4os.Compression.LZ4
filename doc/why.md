## What is 'Fast compression algorithm'?
While compression algorithms you use day-to-day to archive your data work around the speed of 10MB/s giving you quite decent compression ratios, 'fast algorithms' are designed to work 'faster than your hard drive' sacrificing compression ratio.
One of the most famous fast compression algorithms in Google's own [Snappy](http://code.google.com/p/snappy/) which is advertised as 250MB/s compression, 500MB/s decompression on i7 in 64-bit mode.
Fast compression algorithms help reduce network traffic / hard drive load compressing data on the fly with no noticeable latency.

I just tried to compress some sample data (Silesia Corpus) receiving:
* **zlib** (7zip) - 7.5M/s compression, 110MB/s decompression, 44% compression ratio
* **lzma** (7zip) - 1.5MB/s compression, 50MB/s decompression, 37% compression ratio
* **lz4** - 280MB/s compression, 520MB/s decompression, 57% compression ratio

**Note**: Values above are for illustration only. they are affected by HDD read/write speed (in fact LZ4 decompression in much faster). The 'real' tests are taking HDD speed out of equation. For detailed performance tests see [Performance Testing] and [Comparison to other algorithms].

## Other 'Fast compression algorithms'
There are multiple fast compression algorithms, to name a few: [LZO](http://lzohelper.codeplex.com/), [QuickLZ](http://www.quicklz.com/index.php), [LZF](http://csharplzfcompression.codeplex.com/), [Snappy](https://github.com/Kintaro/SnappySharp), FastLZ.
You can find comparison of them on [LZ4 webpage](http://code.google.com/p/lz4/) or [here](http://www.technofumbles.com/weblog/2011/04/22/survey-of-fast-compression-algorithms-part-1-2/)

