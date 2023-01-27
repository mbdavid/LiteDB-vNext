namespace LiteDB.Engine;

/// <summary>
/// Represent a location on data disk from a buffer source
/// </summary>
[DebuggerStepThrough]
internal struct PageDataLocation
{
    /// <summary>
    /// PageID
    /// </summary>
    public readonly uint PageID;

    /// <summary>
    /// Memory buffer instance
    /// </summary>
    public readonly Memory<byte> Buffer;

    public PageDataLocation(uint pageID, Memory<byte> buffer)
    {
        this.PageID = pageID;
        this.Buffer = buffer;
    }

    public override string ToString()
    {
        return $"{this.PageID:0000}";
    }
}