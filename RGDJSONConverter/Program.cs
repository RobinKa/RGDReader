using Microsoft.Extensions.FileSystemGlobbing;
using RGDReader;
using System.Text;

if (args.Length != 2)
{
    return;
}

string path = args[0];
string outPath = args[1];

Console.WriteLine("Searching for rgd files in {0}", path);

Matcher matcher = new();
matcher.AddInclude("**/*.rgd");

var rgdPaths = matcher.GetResultsInFullPath(path).ToArray();
Console.WriteLine("Found {0} rgd files", rgdPaths.Length);

void ConvertRGD(string rgdPath)
{
    var relativePath = Path.GetRelativePath(path, rgdPath);
    var outRelativePath = Path.ChangeExtension(relativePath, "json");
    var outJsonPath = Path.Join(outPath, outRelativePath);

    using var reader = new ChunkyFileReader(File.Open(rgdPath, FileMode.Open), Encoding.ASCII);

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

        StringBuilder stringBuilder = new StringBuilder();
        void printIndent(int indent)
        {
            for (int i = 0; i < indent; i++)
            {
                stringBuilder.Append("  ");
            }
        }
        
        void printTable(IReadOnlyDictionary<ulong, (int Type, object Value)> table, bool comma = false, int indent = 0)
        {
            stringBuilder.Append("{\n");

            var keys = table.Keys.ToArray();
            var count = keys.Length;
            for (int i = 0; i < count; i++)
            {
                var childKey = keys[i];
                var (childType, childValue) = table[childKey];
                printValue(childKey, childType, childValue, i + 1 != count, indent + 1);
            }

            printIndent(indent - 1);
            stringBuilder.Append("}");
            if (comma)
            {
                stringBuilder.Append(",");
            }
            stringBuilder.Append("\n");
        }

        void printValue(ulong key, int type, object value, bool comma = false, int indent = 0)
        {
            printIndent(indent);
            stringBuilder.AppendFormat("\"{0}\": ", keysInv[key]);

            if (value is IReadOnlyDictionary<ulong, (int Type, object Value)> table)
            {
                printTable(table, comma, indent + 1);
            }
            else
            {
                stringBuilder.AppendFormat("{0}", type switch
                {
                    2 => (bool)value ? "true" : "false",
                    3 => $"\"{((string)value).Replace("\\", "\\\\")}\"",
                    _ => value,
                });

                if (comma)
                {
                    stringBuilder.Append(",");
                }

                stringBuilder.Append("\n");
            }
        }

        printTable(kvs.KeyValues);

        Directory.CreateDirectory(Path.GetDirectoryName(outJsonPath));
        File.WriteAllText(outJsonPath, stringBuilder.ToString());
    }
}

using (var progress = new ProgressBar())
{
    int processed = 0;
    foreach (var rgdPath in rgdPaths)
    {
        ConvertRGD(rgdPath);
        processed++;
        progress.Report((double)processed / rgdPaths.Length);
    }
}