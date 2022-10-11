namespace K4os.Compression.LZ4.Streams.Adapters;

/// <summary>
/// Empty record equivalent to Unit/Void.
/// Works as placeholder type used when working with generic interfaces which do require type,
/// but implementation needs none.
/// Please note, whole <c>K4os.Compression.LZ4.Streams.Adapters</c> namespace should be considered
/// pubternal - exposed as public but still very likely to change.
/// </summary>
public readonly struct EmptyState { }
