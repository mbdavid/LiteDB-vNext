namespace LiteDB.Engine;

internal readonly struct SortItem
{
    public static readonly SortItem Empty = new();

    public readonly PageAddress RowID;
    public readonly BsonValue Key;

    public bool IsEmpty => this.RowID.IsEmpty;

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
        return this.IsEmpty ? "<EMPTY>" : $"{{ RowID = {RowID}, Key = {Key} }}";
    }
}
