namespace LiteDB.Engine;

unsafe internal struct InsertNodeResult
{
    public RowID IndexNodeID;
    public IndexNodeLevel* LevelsPtr;
}
