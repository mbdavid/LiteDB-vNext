﻿namespace LiteDB.Engine;

internal struct CheckpointAction
{
    public CheckpointActionEnum Action;
    public int PositionID;
    public int TargetPositionID; // used only in CopyToDataFile and CopyToTemp (MaxValue for ClearPage)
    public bool MustClear; // clear page PositionID

    public override string ToString()
    {
        return $"{{ Action = {Action}, PositionID = {Dump.PageID(PositionID)}, TargetPositionID = {Dump.PageID(TargetPositionID)}, MustClear = {MustClear} }}";
    }
}