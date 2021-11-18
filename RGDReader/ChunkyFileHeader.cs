namespace RGDReader;

public record class ChunkyFileHeader(char[] Magic, int Version, int Platform);