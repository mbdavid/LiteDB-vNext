namespace LiteDB.Engine;

/// <summary>
/// Represent a single page segment with Location (position) and Length
/// </summary>
internal readonly struct PageSegment
{
    /// <summary>
    /// Segment location on page buffer
    /// </summary>
    public readonly ushort Location;

    /// <summary>
    /// Segment length
    /// </summary>
    public readonly ushort Length;

    /// <summary>
    /// Get final location (Location + Length)
    /// </summary>
    public ushort EndLocation => (ushort)(this.Location + this.Length);

    /// <summary>
    /// Indicate this segment are clear (no reference)
    /// </summary>
    public bool IsEmpty => this.Location == 0 && this.Length == 0;

    public PageSegment(ushort location, ushort length)
    {
        this.Location = location;
        this.Length = length;
    }

    /// <summary>
    /// Get a page segment location/length using index
    /// </summary>
    public static PageSegment GetSegment(PageBuffer page, byte index, out PageSegment segmentAddr)
    {
        // get read
        var span = page.AsSpan();

        // read slot address
        segmentAddr = GetSegmentAddr(index);

        // read segment position/length
        var location = span[segmentAddr.Location..].ReadUInt16();
        var length = span[segmentAddr.Length..].ReadUInt16();

        // create new segment based on location and length from page footer
        var segment = new PageSegment(location, length);

        ENSURE(() => page.Header.IsValidSegment(segment), $"Invalid segment {segment}");

        return segment;
    }

    public override string ToString() => this.IsEmpty ? "<EMPTY>" : $"{{ Loc = {this.Location}, Len = {this.Length} }}";

    /// <summary>
    /// Get segment address at footer page. Returns only footer address reference (not real page segment)
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static PageSegment GetSegmentAddr(byte index)
    {
        var locationAddr = PAGE_SIZE - ((index + 1) * PageHeader.SLOT_SIZE) + 2;
        var lengthAddr = PAGE_SIZE - ((index + 1) * PageHeader.SLOT_SIZE);

        return new((ushort)locationAddr, (ushort)lengthAddr);
    }
}
