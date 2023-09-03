namespace LiteDB.Engine;

/// <summary>
/// Represent a single page segment with Location (position) and Length
/// </summary>
unsafe internal struct PageSegment
{
    public ushort Location;  // 2
    public ushort Length;    // 2

    /// <summary>
    /// Get final location (Location + Length)
    /// </summary>
    public ushort EndLocation => (ushort)(this.Location + this.Length);

    /// <summary>
    /// Indicate this segment are clear (no reference)
    /// </summary>
    public bool IsEmpty => this.Location == 0 && this.Length == 0;

    public PageSegment()
    {
    }

    public PageSegment(ushort location, ushort length)
    {
        this.Location = location;
        this.Length = length;
    }

    /// <summary>
    /// Get a page segment location/length using index
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static PageSegment* GetSegment(PageMemory* pagePtr, ushort index)
    {
        var segmentOffset = PAGE_SIZE - (index * sizeof(PageSegment));

        var segmentPtr = (PageSegment*)((nint)pagePtr + segmentOffset);

        return segmentPtr;
    }


    [Obsolete]
    public static PageSegment GetSegment(PageBuffer page, byte index, out PageSegment segmentAddr)
    {
        var span = page.AsSpan();
        segmentAddr = GetSegmentAddr(index);
        var location = span[segmentAddr.Location..].ReadUInt16();
        var length = span[segmentAddr.Length..].ReadUInt16();
        var segment = new PageSegment(location, length);
        return segment;
    }


    [Obsolete]
    public static PageSegment GetSegmentAddr(byte index)
    {
        var locationAddr = PAGE_SIZE - ((index + 1) * PageHeader.SLOT_SIZE) + 2;
        var lengthAddr = PAGE_SIZE - ((index + 1) * PageHeader.SLOT_SIZE);
        return new((ushort)locationAddr, (ushort)lengthAddr);
    }


    public override string ToString() => Dump.Object(this);

}
