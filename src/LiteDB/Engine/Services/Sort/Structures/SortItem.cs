namespace LiteDB.Engine;

internal readonly struct SortItem
{
    public static readonly SortItem Empty = new();

    public readonly PageAddress DataBlockID;
    public readonly BsonValue Key;

    public bool IsEmpty => this.DataBlockID.IsEmpty;

    public SortItem()
    {
        this.DataBlockID = PageAddress.Empty;
        this.Key = BsonValue.Null;
    }

    public SortItem(PageAddress dataBlockID, BsonValue key)
    {
        this.DataBlockID = dataBlockID;
        this.Key = key;
    }

    public int GetBytesCount()
    {
        return __IndexNode.GetKeyLength(this.Key) + PageAddress.SIZE;
    }

    public override string ToString() => Dump.Object(this);
}
