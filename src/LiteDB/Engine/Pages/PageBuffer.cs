namespace LiteDB.Engine;

/// <summary>
/// Should be a class to be used in heap
/// * Shared (thread safe)
/// </summary>
internal class PageBuffer
{
    /// <summary>
    /// Position on disk where this page came from or where this page must be stored
    /// </summary>
    public int PositionID = int.MaxValue;

    /// <summary>
    /// Contains how many threads are sharing this page buffer for read.
    /// If ShareCounter = -1 means this PageBuffer are not shared in cache (can be changed)
    /// If ShareCounter = 0 means this PageBuffer are on cache but no one are using (can be dispose if needed)
    /// If ShareCounter > 0 means this PageBuffer contains 1 or more threads reading this page (can't be changed)
    /// </summary>
    public int ShareCounter = NO_CACHE;

    /// <summary>
    /// Last time this buffer was hit by cache
    /// </summary>
    public long Timestamp = 0;

    /// <summary>
    /// Get/Set if page was modified and need to saved on disk
    /// </summary>
    public bool IsDirty = false;

    /// <summary>
    /// Page header structure. Must be loaded/updated to buffer 
    /// </summary>
    public PageHeader Header = new();

    /// <summary>
    /// Page memory buffer with PAGE_SIZE size
    /// </summary>
    public readonly byte[] Buffer = new byte[PAGE_SIZE];

    public bool IsData => this.PositionID <= this.Header.PageID;
    public bool IsLog => this.PositionID > this.Header.PageID;
    public bool IsTempLog => this.IsLog && this.PositionID != this.Header.PositionID;

    public PageBuffer()
    {
    }

    public void Reset()
    {
        this.ShareCounter = NO_CACHE;
        this.Timestamp = 0;
        this.PositionID = int.MaxValue;
        this.IsDirty = false;
    }

    public Span<byte> AsSpan()
    {
        return this.Buffer.AsSpan(0, PAGE_SIZE);
    }

    public Span<byte> AsSpan(int start)
    {
        return this.Buffer.AsSpan(start);
    }

    public Span<byte> AsSpan(PageSegment segment)
    {
        return this.Buffer.AsSpan(segment.Location, segment.Length);
    }

    public Span<byte> AsSpan(int start, int length)
    {
        return this.Buffer.AsSpan(start, length);
    }

    /// <summary>
    /// Test if first 32 header bytes are zero
    /// </summary>
    public bool IsHeaderEmpty()
    {
        return this.Buffer.AsSpan(0, PAGE_HEADER_SIZE).IsFullZero();
    }

    /// <summary>
    /// Copy buffer content to another PageBuffer and reload Header
    /// </summary>
    public void CopyBufferTo(PageBuffer page)
    {
        // copy content
        this.Buffer.AsSpan().CopyTo(page.Buffer);

        // update page header
        page.Header.ReadFromPage(page);

    }

    public override string ToString() => $"PageID: {Header.PageID} / PositionID: {this.PositionID}";
}
