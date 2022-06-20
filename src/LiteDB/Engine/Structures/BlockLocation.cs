namespace LiteDB.Engine;

/// <summary>
/// Represent a block location inside abstract BlockPage. Used in Footer as pointer to page content
/// </summary>
internal struct BlockLocation
{
    public ushort Position { get; }

    public ushort Length { get; }

    public BlockLocation(ushort position, ushort length)
    {
        this.Position = position;
        this.Length = length;
    }
}
