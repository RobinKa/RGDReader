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

        StringBuilder stringBuilder = new StringBuilder();
        void printIndent(int indent)
        {
            for (int i = 0; i < indent; i++)
            {
                stringBuilder.Append("  ");
            }
        }
        
        void printTable(IList<(ulong Key, int Type, object Value)> table, int indent)
        {
            stringBuilder.Append("[\n");

            for (int i = 0; i < table.Count; i++)
            {
                var (childKey, childType, childValue) = table[i];

                printIndent(indent);
                stringBuilder.Append("{\n");

                printIndent(indent + 1);
                stringBuilder.AppendFormat("\"key\": \"{0}\",\n", keysInv[childKey]);
                printIndent(indent + 1);
                stringBuilder.Append("\"value\": ");
                printValue(childType, childValue, indent + 1);

                printIndent(indent);
                stringBuilder.Append("}");
                if (i != table.Count - 1)
                {
                    stringBuilder.Append(",");
                }
                stringBuilder.Append("\n");
            }

            printIndent(indent - 1);
            stringBuilder.Append("]");
            stringBuilder.Append("\n");
        }

        void printValue(int type, object value, int indent)
        {
            if (value is IList<(ulong Key, int Type, object Value)> table)
            {
                printTable(table, indent + 1);
            }
            else
            {
                stringBuilder.AppendFormat("{0}", type switch
                {
                    2 => (bool)value ? "true" : "false",
                    3 => $"\"{((string)value).Replace("\\", "\\\\")}\"",
                    _ => value,
                });

                stringBuilder.Append("\n");
            }
        }

        printTable(kvs.KeyValues, 1);

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