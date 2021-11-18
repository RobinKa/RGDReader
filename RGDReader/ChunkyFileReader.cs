using System.Text;

namespace RGDReader;
public class ChunkyFileReader : BinaryReader
{
    public ChunkyFileReader(Stream input) : base(input)
    {
    }

    public ChunkyFileReader(Stream input, Encoding encoding) : base(input, encoding)
    {
    }

    public ChunkyFileReader(Stream input, Encoding encoding, bool leaveOpen) : base(input, encoding, leaveOpen)
    {
    }

    public ChunkHeader ReadChunkHeader()
    {
        return new ChunkHeader(
            new string(ReadChars(4)),
            new string(ReadChars(4)),
            ReadInt32(),
            ReadInt32(),
            ReadInt32()
        );
    }

    public ChunkyFileHeader ReadChunkyFileHeader()
    {
        return new ChunkyFileHeader(
            ReadChars(16),
            ReadInt32(),
            ReadInt32()
        );
    }

    public string ReadCString()
    {
        List<char> chars = new List<char>();
        while (true)
        {
            char c = ReadChar();
            if (c != 0)
            {
                chars.Add(c);
            }
            else
            {
                break;
            }
        }

        return new string(chars.ToArray());
    }

    private object ReadType(int type)
    {
        return type switch
        {
            0 => ReadSingle(),
            1 => ReadInt32(),
            2 => ReadBoolean(),
            3 => ReadCString(),
            100 => ReadTable(),
            101 => ReadTable(),
            _ => throw new Exception($"Unknown type {type}")
        };
    }

    private List<(ulong Key, int Type, object Value)> ReadTable()
    {
        int length = ReadInt32();

        // Read table index
        List<(ulong key, int Type, int index)> keyTypeAndDataIndex = new();
        for (int i = 0; i < length; i++)
        {
            ulong key = ReadUInt64();
            int type = ReadInt32();
            int index = ReadInt32();
            keyTypeAndDataIndex.Add((key, type, index));
        }

        // Read table row data
        long dataPosition = BaseStream.Position;

        List<(ulong Key, int Type, object Value)> kvs = new();
        foreach (var (key, type, index) in keyTypeAndDataIndex)
        {
            BaseStream.Position = dataPosition + index;
            kvs.Add((key, type, ReadType(type)));
        }

        return kvs;
    }

    public KeyValueDataChunk ReadKeyValueDataChunk(int length)
    {
        long startPosition = BaseStream.Position;

        int unknown2 = ReadInt32();

        var table = ReadTable();

        BaseStream.Position = startPosition + length;

        return new KeyValueDataChunk(table);
    }

    public KeysDataChunk ReadKeysDataChunk()
    {
        Dictionary<string, ulong> stringKeys = new();

        int count = ReadInt32();

        for (int i = 0; i < count; i++)
        {
            ulong key = ReadUInt64();

            int stringLength = ReadInt32();
            string str = new string(ReadChars(stringLength));

            stringKeys.Add(str, key);
        }

        return new KeysDataChunk(stringKeys);
    }
}