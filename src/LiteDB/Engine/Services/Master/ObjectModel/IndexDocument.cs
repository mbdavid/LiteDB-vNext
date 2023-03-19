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
    /// Create new PK index document
    /// </summary>
    public IndexDocument(PageAddress head, PageAddress tail)
    {
        this.Slot = 0;
        this.Name = "_id";
        this.Expr = BsonExpression.Create("$._id");
        this.Unique = true;
        this.Head = head;
        this.Tail = tail;
        this.Meta = new BsonDocument();
    }

    /// <summary>
    /// Load index document from $master.collections[]
    /// </summary>
    public IndexDocument(byte slot, BsonDocument doc)
    {
        this.Slot = slot;
        this.Name = doc[MK_IDX_NAME];
        this.Expr = BsonExpression.Create(doc[MK_IDX_EXPR]);
        this.Unique = doc[MK_IDX_UNIQUE];
        this.Head = new PageAddress((uint)doc[MK_IDX_HEAD_PAGE_ID], (byte)doc[MK_IDX_HEAD_INDEX]);
        this.Tail = new PageAddress((uint)doc[MK_IDX_TAIL_PAGE_ID], (byte)doc[MK_IDX_TAIL_INDEX]);
        this.Meta = doc[MK_META].AsDocument;
    }
}

