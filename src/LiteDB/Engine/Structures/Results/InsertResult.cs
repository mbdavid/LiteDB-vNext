namespace LiteDB.Engine;

unsafe internal struct InsertResult
{
    public RowID DataBlockID;
    public DataBlock* DataBlockPtr;
}
