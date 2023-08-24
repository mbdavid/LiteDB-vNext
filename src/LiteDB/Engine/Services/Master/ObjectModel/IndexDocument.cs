namespace LiteDB.Engine;

internal class IndexDocument
{
    public byte Slot { get; init; }
    public required string Name { get; init; }
    public required BsonExpression Expression { get; init; }
    public bool Unique { get; init; }
    public PageAddress HeadIndexNodeID { get; init; }
    public PageAddress TailIndexNodeID { get; init; }

    public IndexDocument()
    {
    }

    /// <summary>
    /// Clone object instance constructor
    /// </summary>
    public IndexDocument(IndexDocument other)
    {
        this.Slot = other.Slot;
        this.Name = other.Name;
        this.Expression = other.Expression;
        this.Unique = other.Unique;
        this.HeadIndexNodeID = other.HeadIndexNodeID;
        this.TailIndexNodeID = other.TailIndexNodeID;
    }
}

