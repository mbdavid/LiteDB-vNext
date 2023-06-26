namespace LiteDB.Engine;

internal struct SortItem
{
    public BsonValue Key;
    public PageAddress DataBlock;
}
