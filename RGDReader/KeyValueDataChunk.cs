namespace RGDReader;

public record class KeyValueDataChunk(IReadOnlyDictionary<ulong, (int Type, object Value)> KeyValues);