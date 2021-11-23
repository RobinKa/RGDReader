using Microsoft.Extensions.FileSystemGlobbing;
using RGDReader;
using System.Text;

if (args.Length != 2)
{
    Console.WriteLine("Usage: {0} <Directory with RGDs> <Output directory>", AppDomain.CurrentDomain.FriendlyName);
    return;
}

string path = args[0];
string outPath = args[1];

Console.WriteLine("Output directory: {0}", outPath);
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

    var nodes = ChunkyUtil.ReadRGD(rgdPath);

    StringBuilder stringBuilder = new StringBuilder();
    void printIndent(int indent)
    {
        for (int i = 0; i < indent; i++)
        {
            stringBuilder.Append("  ");
        }
    }

    void printValue(RGDNode node, int depth)
    {
        printIndent(depth);
        stringBuilder.Append("{\n");
        printIndent(depth + 1);
        stringBuilder.AppendFormat("\"key\": \"{0}\",\n", node.Key);
        printIndent(depth + 1);
        stringBuilder.Append("\"value\": ");

        if (node.Value is IList<RGDNode> childNodes)
        {
            stringBuilder.Append("[\n");

            for (int i = 0; i < childNodes.Count; i++)
            {
                printValue(childNodes[i], depth + 2);
                var childNode = childNodes[i];

                if (i != childNodes.Count - 1)
                {
                    stringBuilder.Append(",");
                }

                stringBuilder.Append("\n");
            }

            printIndent(depth + 1);
            stringBuilder.Append("]");
            stringBuilder.Append("\n");
        }
        else
        {
            stringBuilder.Append(node.Value switch
            {
                bool b => b ? "true" : "false",
                string s => $"\"{s.Replace("\\", "\\\\")}\"",
                _ => node.Value,
            });

            stringBuilder.Append("\n");
        }

        printIndent(depth);
        stringBuilder.Append("}");
    }

    stringBuilder.Append("{\n");
    printIndent(1);
    stringBuilder.Append("\"data\": [\n");
    for (int i = 0; i < nodes.Count; i++)
    {
        printValue(nodes[i], 2);
        if (i != nodes.Count - 1)
        {
            stringBuilder.Append(",");
        }
        stringBuilder.Append("\n");
    }
    printIndent(1);
    stringBuilder.Append("]\n}");

    Directory.CreateDirectory(Path.GetDirectoryName(outJsonPath));
    File.WriteAllText(outJsonPath, stringBuilder.ToString());
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