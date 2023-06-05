namespace LiteDB.Engine;

internal class IndexDocument
{
    public byte Slot { get; }
    public string Name { get; }
    public BsonExpression Expr { get; }
    public bool Unique { get; }
    public PageAddress Head { get; }
    public PageAddress Tail { get; }
    public BsonDocument Meta { get; }

    /// <summary>
    /// Load index document from $master.collections[0].indexes
    /// </summary>
    public IndexDocument(string name, BsonDocument doc)
    {
        this.Name = name;
        this.Slot = (byte)doc[MK_IDX_SLOT].AsInt32;
        this.Expr = BsonExpression.Create(doc[MK_IDX_EXPR]);
        this.Unique = doc[MK_IDX_UNIQUE];
        this.Head = new PageAddress((uint)doc[MK_IDX_HEAD_PAGE_ID], (byte)doc[MK_IDX_HEAD_INDEX]);
        this.Tail = new PageAddress((uint)doc[MK_IDX_TAIL_PAGE_ID], (byte)doc[MK_IDX_TAIL_INDEX]);
        this.Meta = doc[MK_META].AsDocument;
    }
}

