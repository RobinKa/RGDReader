using RGDReader;
using System.Text;
using System.IO.Compression;
using System.Drawing;
using BCnEncoder.Decoder;
using BCnEncoder.Shared;
using Microsoft.Extensions.FileSystemGlobbing;

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
matcher.AddInclude("**/*.rrtex");

var rgdPaths = matcher.GetResultsInFullPath(path).ToArray();
Console.WriteLine("Found {0} rgd files", rgdPaths.Length);

void ConvertRRTex(string rrtexPath)
{
    var reader = new ChunkyFileReader(File.Open(rrtexPath, FileMode.Open), Encoding.ASCII);
    var fileHeader = reader.ReadChunkyFileHeader();

    int mipCount = -1;
    int width = 0;
    int height = 0;
    int[] mipTextureCounts = null;
    List<int> sizeCompressed = null;
    List<int> sizeUncompressed = null;
    int textureCompression = -1;

    void PrintHeaders(long position, long length)
    {
        var relativePath = Path.GetRelativePath(path, rrtexPath);
        var outRelativePath = Path.ChangeExtension(relativePath, null);

        reader.BaseStream.Position = position;
        foreach (var header in reader.ReadChunkHeaders(length))
        {
            long pos = reader.BaseStream.Position;
            reader.BaseStream.Position = header.DataPosition;

            if (header.Name == "TMAN")
            {
                int unknown1 = reader.ReadInt32();
                width = reader.ReadInt32();
                height = reader.ReadInt32();
                int unknown2 = reader.ReadInt32();
                int unknown3 = reader.ReadInt32();
                textureCompression = reader.ReadInt32();

                mipCount = reader.ReadInt32();
                int unknown5 = reader.ReadInt32();

                mipTextureCounts = new int[mipCount];
                for (int i = 0; i < mipCount; i++)
                {
                    mipTextureCounts[i] = reader.ReadInt32();
                }

                sizeCompressed = new();
                sizeUncompressed = new();

                for (int i = 0; i < mipCount; i++)
                {
                    for (int j = 0; j < mipTextureCounts[i]; j++)
                    {
                        sizeUncompressed.Add(reader.ReadInt32());
                        sizeCompressed.Add(reader.ReadInt32());
                    }
                }
            }

            if (header.Name == "TDAT")
            {
                int count = 0;
                int unk = reader.ReadInt32();
                for (int i = 0; i < mipCount; i++)
                {
                    List<byte> data = new();
                    int w = -1;
                    int h = -1;

                    for (int j = 0; j < mipTextureCounts[i]; j++)
                    {
                        long prePos = reader.BaseStream.Position;

                        if (sizeCompressed[count] != sizeUncompressed[count])
                        {
                            byte[] zlibHeader = reader.ReadBytes(2);

                            DeflateStream deflateStream = new DeflateStream(reader.BaseStream, CompressionMode.Decompress, true);
                            MemoryStream inflatedStream = new MemoryStream();
                            deflateStream.CopyTo(inflatedStream);
                            int length2 = (int)inflatedStream.Length;

                            BinaryReader dataReader = new BinaryReader(inflatedStream);
                            dataReader.BaseStream.Position = 0;

                            if (j == 0)
                            {
                                int mipLevel = dataReader.ReadInt32();
                                int widthx = dataReader.ReadInt32();
                                int heightx = dataReader.ReadInt32();
                                w = Math.Max(widthx, 4);
                                h = Math.Max(heightx, 4);
                                int numPhysicalTexels = dataReader.ReadInt32();
                                data.AddRange(dataReader.ReadBytes(length2 - 16));
                            }
                            else
                            {
                                data.AddRange(dataReader.ReadBytes(length2));
                            }
                        }
                        else
                        {
                            int length2 = sizeUncompressed[count];

                            if (j == 0)
                            {
                                int mipLevel = reader.ReadInt32();
                                int widthx = reader.ReadInt32();
                                int heightx = reader.ReadInt32();
                                w = Math.Max(widthx, 4);
                                h = Math.Max(heightx, 4);
                                int numPhysicalTexels = reader.ReadInt32();
                                data.AddRange(reader.ReadBytes(length2 - 16));
                            }
                            else
                            {
                                data.AddRange(reader.ReadBytes(length2));
                            }
                        }

                        reader.BaseStream.Position = prePos + sizeCompressed[count];

                        count++;
                    }

                    var decoder = new BcDecoder();


                    var format = textureCompression switch
                    {
                        18 => CompressionFormat.Bc1WithAlpha,
                        19 => CompressionFormat.Bc1,
                        22 => CompressionFormat.Bc3,
                        //23 => CompressionFormat.bc,
                        //28 => CompressionFormat.Bc5,
                        _ => CompressionFormat.Unknown
                    };

                    if (format == CompressionFormat.Unknown)
                    {
                        Console.WriteLine("Unknown texture compression method {0} for {1}, w={2} h={3} comp={4} uncomp={5}", textureCompression, rrtexPath, w, h, sizeCompressed[0], sizeUncompressed[0]);
                        continue;
                    }

                    var outColors = decoder.DecodeRaw(data.ToArray(), w, h, format);

                    Bitmap bitmap = new Bitmap(w, h);
                    for (int x = 0; x < w; x++)
                    {
                        for (int y = 0; y < h; y++)
                        {
                            var color = outColors[x + w * y];

                            if (textureCompression == 18 || textureCompression == 19)
                            {
                                bitmap.SetPixel(x, y, Color.FromArgb(color.a, color.r, color.g, color.b));
                            }
                            else
                            {
                                bitmap.SetPixel(x, y, Color.FromArgb(255 - color.a, color.r, color.g, color.b));
                            }
                        }
                    }

                    var outImagePath = Path.Join(outPath, $"{outRelativePath}_mip{i}.png");
                    Directory.CreateDirectory(Path.GetDirectoryName(outImagePath));
                    bitmap.Save(outImagePath);

                    width = Math.Max(1, width / 2);
                    height = Math.Max(1, height / 2);
                }
            }

            reader.BaseStream.Position = pos;

            if (header.Type == "FOLD")
            {
                PrintHeaders(header.DataPosition, header.Length);
            }
        }
    }

    PrintHeaders(reader.BaseStream.Position, reader.BaseStream.Length - reader.BaseStream.Position);
}

using (var progress = new ProgressBar())
{
    int processed = 0;
    foreach (var rgdPath in rgdPaths)
    {
        ConvertRRTex(rgdPath);
        processed++;
        progress.Report((double)processed / rgdPaths.Length);
    }
}