using RGDReader;
using System.Text;

if (args.Length != 1)
{
    Console.WriteLine("Usage: {0} <RGD File Path>", AppDomain.CurrentDomain.FriendlyName);
    return;
}

string rgdPath = args[0];

var nodes = ChunkyUtil.ReadRGD(rgdPath);

static void PrintNode(RGDNode node, int depth = 0)
{
    for (int i = 0; i < depth; i++)
    {
        Console.Write("\t");
    }
    Console.Write(node.Key);
    if (node.Value is IList<RGDNode> children)
    {
        Console.WriteLine();
        foreach (var child in children)
        {
            PrintNode(child, depth + 1);
        }
    }
    else
    {
        Console.Write(": ");
        Console.WriteLine(node.Value);
    }
}

foreach (var node in nodes)
{
    PrintNode(node);
}
