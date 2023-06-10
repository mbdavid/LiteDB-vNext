namespace LiteDB.Engine;

internal class IndexDocument
{
    public byte Slot { get; init; }
    public string Name { get; init; }
    public BsonExpression Expr { get; init; }
    public bool Unique { get; init; }
    public PageAddress Head { get; init; }
    public PageAddress Tail { get; init; }

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
        this.Expr = other.Expr;
        this.Unique = other.Unique;
        this.Head = other.Head;
        this.Tail = other.Tail;
    }
}

