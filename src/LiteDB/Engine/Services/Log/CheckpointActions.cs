﻿namespace LiteDB.Engine;

internal struct CheckpointAction
{
    public CheckpointActionEnum Action;
    public int PositionID;
    public int TargetPositionID; // used only in CopyToDataFile and CopyToTemp (MaxValue for ClearPage)
    public bool MustClear; // clear page PositionID
}

internal enum CheckpointActionEnum : byte
{
    CopyToDataFile = 0,
    CopyToTempFile = 1,
    ClearPage = 2
}

internal class CheckpointActions
{
    public IList<CheckpointAction> GetActions(
        IReadOnlyList<PageHeader> logPages,
        HashSet<int> confirmedTransactions,
        int lastPageID,
        int startTempPositionID,
        IList<PageHeader> tempPages)
    {

        //TODO: Lucas
        //throw new NotImplementedException();

        return new List<CheckpointAction>();
    }
}