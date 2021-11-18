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

    public static (int Type, object Value)? ResolveKey(ulong key, KeyValueDataChunk kvs)
    {
        if (kvs.KeyValues.TryGetValue(key, out var result))
        {
            return result;
        }

        return null;
    }

    public static IDictionary<string, (int Type, object Value)?> ResolveKeyValues(KeysDataChunk keys, KeyValueDataChunk kvs)
    {
        Dictionary<string, (int Type, object Value)?> resolved = new();
        foreach (var (stringKey, key) in keys.StringKeys)
        {
            resolved[stringKey] = ResolveKey(key, kvs);
        }
        return resolved;
    }
}