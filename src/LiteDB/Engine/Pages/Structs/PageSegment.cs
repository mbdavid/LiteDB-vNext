namespace LiteDB.Engine;

/// <summary>
/// Represent a single page segment with Location (position) and Length
/// </summary>
internal struct PageSegment
{
    /// <summary>
    /// Segment location on page buffer
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

        // read segment location/length
        var location = span[segmentAddr.Location..2].ReadUInt16();
        var length = span[segmentAddr.Length..2].ReadUInt16();

        var segment = new PageSegment(location, length);

        ENSURE(!page.Header.IsValidSegment(segment), $"invalid segment {segment} on page {page}");

        // return buffer slice with content only data
        return segment;
    }

    public override string ToString() => $"{this.Location}/{this.Length}";

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
