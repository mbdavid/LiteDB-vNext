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
    public static PageSegment* GetSegment(PageMemory* page, ushort index)
    {
        var segmentOffset = PAGE_SIZE - (index * sizeof(PageSegment));

        var segment = (PageSegment*)((nint)page + segmentOffset);

        return segment;
    }

    public override string ToString() => Dump.Object(this);

}
