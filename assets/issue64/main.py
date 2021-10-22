import lz4.block
import struct

in_file = open("issue64.in", "rb")
out_file = open("issue64.out", "wb")
block_data = in_file.read()
chunk_start=20
last_uncompressed = b'\0'*70000
header = b""
   
while (16384 > chunk_start) and (header != b'bv4$'):  # b'bv41':   
    uncompressed_size, compressed_size = struct.unpack('<II', block_data[chunk_start + 4:chunk_start + 12])
    last_uncompressed = lz4.block.decompress(
        block_data[chunk_start + 12: chunk_start + 12 + compressed_size], 
        uncompressed_size, 
        dict=last_uncompressed
    )
    out_file.write(last_uncompressed)
    chunk_start += 12 + compressed_size
    header = block_data[chunk_start:chunk_start + 4]
