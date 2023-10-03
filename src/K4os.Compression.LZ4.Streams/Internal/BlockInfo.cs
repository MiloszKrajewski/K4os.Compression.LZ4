using K4os.Compression.LZ4.Encoders;

namespace K4os.Compression.LZ4.Streams.Internal;

internal readonly struct BlockInfo
{
	private readonly byte[] _buffer;
	private readonly int _length;

	public byte[] Buffer => _buffer;
	public int Offset => 0;
	public int Length => Math.Abs(_length);
	public bool Compressed => _length > 0;
	public bool Ready => _length != 0;

	public BlockInfo(byte[] buffer, EncoderAction action, int length)
	{
		_buffer = buffer;
		_length = action switch {
			EncoderAction.Encoded => length,
			EncoderAction.Copied => -length,
			_ => 0,
		};
	}
}