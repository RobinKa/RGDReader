using System.Text;

namespace RGDReader;

public static class ChunkyUtil
{
    public static IReadOnlyDictionary<TValue, TKey> ReverseReadOnlyDictionary<TKey, TValue>(IReadOnlyDictionary<TKey, TValue> source) where TValue : notnull
    {
        var dictionary = new Dictionary<TValue, TKey>();
        foreach (var entry in source)
        {
            if (!dictionary.ContainsKey(entry.Value))
                dictionary.Add(entry.Value, entry.Key);
        }
        return dictionary;
    }

    public static IList<RGDNode> ReadRGD(string rgdPath)
    {
        using var reader = new ChunkyFileReader(File.Open(rgdPath, FileMode.Open), Encoding.ASCII);

        reader.ReadChunkyFileHeader();
        var chunkHeaders = reader.ReadChunkHeaders().ToArray();

        ChunkHeader[] keysHeaders = chunkHeaders.Where(header => header.Type == "DATA" && header.Name == "KEYS").ToArray();
        ChunkHeader[] kvsHeaders = chunkHeaders.Where(header => header.Type == "DATA" && header.Name == "AEGD").ToArray();

        if (keysHeaders.Length == 0)
        {
            throw new Exception("No DATA KEYS chunk present");
        }

        if (keysHeaders.Length > 1)
        {
            throw new Exception("More than one DATA KEYS chunk present");
        }

        if (kvsHeaders.Length == 0)
        {
            throw new Exception("No DATA AEGD chunk present");
        }

        if (kvsHeaders.Length > 1)
        {
            throw new Exception("More than one DATA AEGD chunk present");
        }

        var keys = reader.ReadKeysDataChunk(keysHeaders[0]);
        var kvs = reader.ReadKeyValueDataChunk(kvsHeaders[0]);

        var keysInv = ReverseReadOnlyDictionary(keys.StringKeys);

        static RGDNode makeNode(ulong key, object value, IReadOnlyDictionary<ulong, string> keysInv)
        {
            string keyStr = keysInv[key];

            if (value is ChunkyList table)
            {
                return new RGDNode(keyStr, table.Select(listItem => makeNode(listItem.Key, listItem.Value, keysInv)).ToArray());
            }

            return new RGDNode(keyStr, value);
        }

        return kvs.KeyValues.Select(kv => makeNode(kv.Key, kv.Value, keysInv)).ToArray();
    }
}