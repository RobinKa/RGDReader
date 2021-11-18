namespace RGDReader;

public record class KeyValueDataChunk(IList<(ulong Key, int Type, object Value)> KeyValues);