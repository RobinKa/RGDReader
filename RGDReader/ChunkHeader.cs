namespace RGDReader;

public record class ChunkHeader(string Type, string Name, int Version, int Length, int MinVersion);