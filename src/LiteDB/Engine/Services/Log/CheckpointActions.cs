namespace LiteDB.Engine;

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
    public IEnumerable<CheckpointAction> GetActions(
        IReadOnlyList<PageHeader> logPages,
        HashSet<int> confirmedTransactions,
        int lastPageID,
        int startTempPositionID,
        IList<PageHeader> tempPages)
    {
        // get last file position ID
        var lastFilePositionID = tempPages.Count > 0 ?
            startTempPositionID + tempPages.Count - 1 :
            logPages.Max(x => x.PositionID);

        // get first positionID on log (or temp)
        var firstPositionID = Math.Min(logPages[0].PositionID, 
            tempPages.Count > 0 ? tempPages.Select(x => x.PositionID).Min() : logPages[0].PositionID);

        var lastPositionID = logPages[^1].PositionID;
        var lastTempPositionID = startTempPositionID + (tempPages.Count - 1);

        foreach(var logPage in logPages)
        {
            // check if this page will be override in future
            var willOverride = logPages
                .Where(x => x.PositionID > logPage.PositionID)
                .Any(x => x.PageID == logPage.PageID);

            // ** verifica se o log nao esta no temp

            // if page is not confirmed or will be override
            if (willOverride || !confirmedTransactions.Contains(logPage.TransactionID))
            {
                // if page is inside datafile must be clear
                if (logPage.PositionID < lastPageID)
                {
                    yield return new CheckpointAction
                    {
                        Action = CheckpointActionEnum.ClearPage,
                        PositionID = logPage.PositionID
                    };
                }
            }

            // if page can be copied directly to datafile (with no temp)
            else if (logPage.PageID <= logPage.PositionID || logPage.PageID > lastPositionID)
            {
                yield return new CheckpointAction
                {
                    Action = CheckpointActionEnum.CopyToDataFile,
                    PositionID = logPage.PositionID,
                    TargetPositionID = logPage.PageID,
                    MustClear = (logPage.PageID <= lastPageID)
                };
            }

            // if page target must be copied to temp if exists on log 
            else if (logPage.PageID > logPage.PositionID)
            {
                // find target log page
                var target = logPages.FirstOrDefault(x => x.PositionID == logPage.PageID);

                if (target.Equals(default(PageHeader)))
                {
                    // target not found, can copy directly to datafile
                    yield return new CheckpointAction
                    {
                        Action = CheckpointActionEnum.CopyToDataFile,
                        PositionID = logPage.PositionID,
                        TargetPositionID = logPage.PageID,
                        MustClear = (logPage.PageID <= lastPageID)
                    };
                }
                else
                { 
                    // copy target page to temp
                    yield return new CheckpointAction
                    {
                        Action = CheckpointActionEnum.CopyToTempFile,
                        PositionID = target.PositionID,
                        TargetPositionID = lastTempPositionID++,
                        MustClear = false
                    };

                    // copy page to target
                    yield return new CheckpointAction
                    {
                        Action = CheckpointActionEnum.CopyToDataFile,
                        PositionID = logPage.PositionID,
                        TargetPositionID = logPage.PageID,
                        MustClear = (logPage.PageID <= lastPageID)
                    };
                }
            }

            throw new NotImplementedException();

        }
    }
}
