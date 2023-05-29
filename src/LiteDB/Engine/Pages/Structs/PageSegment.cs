namespace LiteDB.Engine;

internal struct PageSegment
{
    /// <summary>
    /// Location on page buffer where this segment start
    /// </summary>
    public readonly int Location;

    /// <summary>
    /// Segment length
    /// </summary>
    public readonly int Length;

    public PageSegment(int location, int length)
    {
        this.Location = location;
        this.Length = length;
    }

    /// <summary>
    /// Get a page segment location/length using index
    /// </summary>
    public static PageSegment GetSegment(PageBuffer page, byte index)
    {
        // get read
        var span = page.AsSpan();

        // read slot address
        var segmentAddr = GetSegmentAddr(index);

        // read segment position/length
        var location = span[segmentAddr.Location..2].ReadUInt16();
        var length = span[segmentAddr.Length..2].ReadUInt16();

        // return buffer slice with content only data
        return new(location, length);
    }

    /// <summary>
    /// Get segment address at footer page. Returns only footer address reference (not real page segment)
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static PageSegment GetSegmentAddr(byte index)
    {
        var locationAddr = PAGE_SIZE - ((index + 1) * PageHeader.SLOT_SIZE) + 2;
        var lengthAddr = PAGE_SIZE - ((index + 1) * PageHeader.SLOT_SIZE);

        return new(locationAddr, lengthAddr);
    }
}
