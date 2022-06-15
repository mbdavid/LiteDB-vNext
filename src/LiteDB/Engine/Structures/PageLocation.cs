namespace LiteDB.Engine;

/// <summary>
/// Represent a location on disk with Memory buffer
/// </summary>
[DebuggerStepThrough]
internal struct PageLocation
{
    /// <summary>
    /// Location on disk (in bytes)
    /// </summary>
    public long Position;

    /// <summary>
    /// PageID
    /// </summary>
    public uint PageID;

    /// <summary>
    /// Memory buffer instance
    /// </summary>
    public Memory<byte> Buffer;

    public PageLocation(Memory<byte> buffer, uint pageID)
    {
        this.PageID = pageID;
        this.Buffer = buffer;
        this.Position = long.MaxValue; // empty
    }

    public override string ToString()
    {
        return $"{this.PageID:0000}:{this.Position}";
    }
}