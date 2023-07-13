namespace LiteDB.Engine;

internal struct SortItem
{
    public PageAddress RowID;
    public BsonValue Key;

    public bool IsEmpty => this.RowID.IsEmpty;

    public static SortItem Empty = new();

    public SortItem()
    {
        this.RowID = PageAddress.Empty;
        this.Key = BsonValue.Null;
    }

    public SortItem(PageAddress rowID, BsonValue key)
    {
        this.RowID = rowID;
        this.Key = key;
    }

    public int GetBytesCount()
    {
        return IndexNode.GetKeyLength(this.Key) + PageAddress.SIZE;
    }

    public override string ToString()
    {
        return $"[{this.RowID}] = {this.Key}";
    }
}
