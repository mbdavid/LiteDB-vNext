namespace LiteDB.Engine;

internal struct CheckpointAction
{
    public CheckpointActionEnum Action;
    public int PositionID;
    public int TargetPositionID; // used only in CopyToDataFile and CopyToTemp (MaxValue for ClearPage)
    public bool MustClear; // clear page PositionID
    //public bool AddToCache;
    //public bool RemoveFromCache;
    //public bool DeallocatePage;
}

internal enum CheckpointActionEnum : byte
{
    CopyToDataFile = 0,
    CopyToTempFile = 1,
    ClearPage = 2
}