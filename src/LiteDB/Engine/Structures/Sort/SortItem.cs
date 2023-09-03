namespace LiteDB.Engine;

internal readonly struct SortItem
{
    public readonly RowID DataBlockID;
    public readonly IndexKey Key;

    public static readonly SortItem Empty = new();

    public bool IsEmpty => this.DataBlockID.IsEmpty;

    public SortItem()
    {
        this.DataBlockID = RowID.Empty;
        this.Key = IndexKey.MinValue;
    }

    public SortItem(RowID dataBlockID, BsonValue key)
    {
        this.DataBlockID = dataBlockID;
        this.Key = key;
    }

    public unsafe int GetBytesCount()
    {
        throw new NotImplementedException();
//        return IndexNode.GetKeyLength(this.Key) + sizeof(RowID);
    }

    public override string ToString() => Dump.Object(this);
}
