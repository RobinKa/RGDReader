using RGDReader;
using System.Text;

if (args.Length != 1)
{
    Console.WriteLine("Usage: {0} <File Path>", AppDomain.CurrentDomain.FriendlyName);
    return;
}

string rgdPath = args[0];

var reader = new ChunkyFileReader(File.Open(rgdPath, FileMode.Open), Encoding.ASCII);
var fileHeader = reader.ReadChunkyFileHeader();
Console.WriteLine("File header version: {0}", fileHeader.Version);

void PrintHeaders(long position, long length, int depth = 0)
{
    reader.BaseStream.Position = position;
    foreach (var header in reader.ReadChunkHeaders(length))
    {
        for (int i = 0; i < depth; i++)
        {
            Console.Write("\t");
        }

        Console.WriteLine("Chunk Type: {0}, Name: {1}, Data length: {2}, Data position: {3}, Version: {4}, Path: {5}",
            header.Type, header.Name, header.Length, header.DataPosition, header.Version, header.Path);

        if (header.Type == "FOLD")
        {
            PrintHeaders(header.DataPosition, header.Length, depth + 1);
        }
    }
}

PrintHeaders(reader.BaseStream.Position, reader.BaseStream.Length - reader.BaseStream.Position);