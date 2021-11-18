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
    Console.WriteLine("All key-values");

    void printTable(IList<(ulong Key, int Type, object Value)> table, int indent = 0)
    {
        foreach (var (childKey, childType, childValue) in table)
        {
            printValue(childKey, childType, childValue, indent + 1);
        }
    }

    void printValue(ulong key, int type, object value, int indent = 0)
    {
        Console.Write(string.Join("", Enumerable.Range(0, indent).Select(_ => "  ")));
        Console.Write(keysInv.GetValueOrDefault(key, "<Unknown>"));
        if (value is IList<(ulong Key, int Type, object Value)> table)
        {
            Console.WriteLine(" [Table]");
            printTable(table, indent + 1);
        }
        else
        {
            Console.WriteLine(" [{0}] <{1}>", typeDisplayName[type], value);
        }
    }

    printTable(kvs.KeyValues);
}