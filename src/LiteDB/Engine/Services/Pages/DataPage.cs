namespace LiteDB.Engine;

unsafe internal static class DataPage
{
    public static void Initialize(PageMemory* pagePtr, uint pageID, byte colID)
    {
        pagePtr->PageID = pageID;
        pagePtr->PageType = PageType.Data;
        pagePtr->ColID = colID;

        pagePtr->IsDirty = true;
    }

    public static DataBlock* InsertDataBlock(PageMemory pagePtr, Span<byte> content, bool extend)
    {


        return (DataBlock*)0;
    }
}
