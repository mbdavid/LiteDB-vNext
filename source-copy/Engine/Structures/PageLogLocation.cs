namespace LiteDB.Engine;

/// <summary>
/// Represent a location on log disk with Memory buffer
/// </summary>
[DebuggerStepThrough]
internal struct PageLogLocation
{
    /// <summary>
    /// Location on log disk - used to trasport after write on wal (MaxValue == empty)
    /// </summary>
    public long Position;

    /// <summary>
    /// PageID
    /// </summary>
    public readonly uint PageID;

    /// <summary>
    /// Memory buffer instance
    /// </summary>
    public readonly Memory<byte> Buffer;

    public PageLogLocation(uint pageID, Memory<byte> buffer)
    {
        this.Position = long.MaxValue; // empty
        this.PageID = pageID;
        this.Buffer = buffer;
    }

    public override string ToString()
    {
        return $"{this.PageID:0000}:{this.Position}";
    }
}