namespace LiteDB.Engine;

unsafe internal struct DataBlockResult
{
    public RowID DataBlockID;
    public PageMemory* Page;
    public DataBlock* DataBlock;
    public byte* DataBuffer;

    public static DataBlockResult Empty = new() { DataBlockID = RowID.Empty };

    public bool IsEmpty => this.DataBlockID.IsEmpty;

    public int DataLength => 0;
    public int DocumentLength => throw new NotImplementedException();

    public DataBlockResult(RowID dataBlockID, PageMemory* page, DataBlock* dataBlock, byte* dataBuffer)
    {
        this.DataBlockID = dataBlockID;
        this.Page = page;
        this.DataBlock = dataBlock;
        this.DataBuffer = dataBuffer;
    }

}
