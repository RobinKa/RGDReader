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
            _ => throw new Exception()
        };
    }

    private Dictionary<ulong, (int Type, object Value)> ReadTable()
    {
        int length = ReadInt32();

        // Read table index
        Dictionary<ulong, (int Type, int index)> keyTypeAndDataIndex = new();
        for (int i = 0; i < length; i++)
        {
            ulong key = ReadUInt64();
            int type = ReadInt32();
            int index = ReadInt32();
            keyTypeAndDataIndex[key] = (type, index);
        }

        // Read table row data
        long dataPosition = BaseStream.Position;

        Dictionary<ulong, (int Type, object Value)> kvs = new();
        foreach (var (key, (type, index)) in keyTypeAndDataIndex)
        {
            BaseStream.Position = dataPosition + index;
            kvs[key] = (type, ReadType(type));
        }

        return kvs;
    }

    public KeyValueDataChunk ReadKeyValueDataChunk(int length)
    {
        long startPosition = BaseStream.Position;

        int unknown2 = ReadInt32();

        var table = ReadTable();

        Dictionary<ulong, (int Type, object Value)> flatTable = new();

        void addToFlatTable(Dictionary<ulong, (int Type, object Value)> t)
        {
            foreach (var (key, tv) in t)
            {
                flatTable[key] = tv;
                if (tv.Type == 100 || tv.Type == 101)
                {
                    addToFlatTable((Dictionary<ulong, (int Type, object Value)>)tv.Value);
                }
            }
        }
        addToFlatTable(table);

        BaseStream.Position = startPosition + length;

        return new KeyValueDataChunk(flatTable);
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