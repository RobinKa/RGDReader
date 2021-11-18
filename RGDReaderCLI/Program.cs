using RGDReader;
using System.Text;

IReadOnlyDictionary<int, string> typeDisplayName = new Dictionary<int, string>
{
    [0] = "Float",
    [1] = "Integer",
    [2] = "Boolean",
    [3] = "String",
    [100] = "Table",
    [101] = "List",
};

string rgdPath = args[0];

using var reader = new ChunkyFileReader(File.Open(rgdPath, FileMode.Open), Encoding.ASCII);

if (args.Length != 1)
{
    Console.WriteLine("Usage: {0} <RGD File Path>", AppDomain.CurrentDomain.FriendlyName);
    return;
}

Console.WriteLine("Reading {0}", args[0]);

var fileHeader = reader.ReadChunkyFileHeader();

KeyValueDataChunk? kvs = null;
KeysDataChunk? keys = null;

while (reader.BaseStream.Position < reader.BaseStream.Length)
{
    var chunkHeader = reader.ReadChunkHeader();
    if (chunkHeader.Type == "DATA")
    {
        if (chunkHeader.Name == "AEGD")
        {
            kvs = reader.ReadKeyValueDataChunk(chunkHeader.Length);
        }

        if (chunkHeader.Name == "KEYS")
        {
            keys = reader.ReadKeysDataChunk();
            break;
        }
    }
}

if (kvs != null && keys != null)
{
    var keysInv = ChunkyUtil.ReverseReadOnlyDictionary(keys.StringKeys);
    var resolved = ChunkyUtil.ResolveKeyValues(keys, kvs);
    Console.WriteLine("All key-values");
    foreach (var kv in resolved)
    {
        if (kv.Value.HasValue)
        {
            Console.WriteLine(
                "{0}: [{1}] <{2}>",
                kv.Key,
                typeDisplayName[kv.Value.Value.Type],
                kv.Value.Value.Value is IReadOnlyDictionary<ulong, (int Type, object Value)> dict ?
                    string.Join(", ", dict.Keys.Select(key => keysInv.GetValueOrDefault(key, "<Unknown>"))) :
                    kv.Value.Value.Value
            );
        }
    }
}