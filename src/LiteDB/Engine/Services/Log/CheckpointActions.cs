namespace LiteDB.Engine;

internal struct CheckpointAction
{
    public CheckpointActionEnum Action;
    public int PositionID;
    public int TargetPositionID; // used only in CopyToDataFile and CopyToTemp (MaxValue for ClearPage)
    public bool MustClear; // clear page PositionID
}

internal struct LogPosition
{
    public int PositionID;
    public int PageID;
    public int PhysicalID;
    public bool IsConfirmed;

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

        // create dict with all duplicates pageID getting last positionID
        var duplicates = logPages
            .Where(x => confirmedTransactions.Contains(x.TransactionID))
            .GroupBy(x => x.PageID)
            .Where(x => x.Count() > 1)
            .Select(x => new { PageID = x.Key, PositionID = x.Max(y => y.PositionID) })
            .ToDictionary(x => x.PageID, x => x);

        var logPositions = new ()[0]; // tem q ter o tamanho do log considerando o temp

        var allPages = logPages
            .Select(x => new LogPosition
            {
                PositionID = x.PositionID,
                PageID = x.PageID,
                PhysicalID = x.PositionID,
                IsConfirmed = confirmedTransactions.Contains(x.TransactionID)
            })
            .ToList();

        var logs = new List<LogPosition>();

        foreach(var tempPage in tempPages)
        {

        }



        //foreach(var logPage in logPages)
        for(var i = 0; i < logPositions.Length; i++)
        {
            var logPage = logPositions[i];

            var willOverride = false;

            // check if this page will be override in future
            if (duplicates.TryGetValue(logPage.PageID, out var lastDuplicate))
            {
                willOverride = logPage.PositionID < lastDuplicate.PositionID;
            }

            // ** verifica se o log nao esta no temp

            // if page is not confirmed or will be override
            if (willOverride || !logPage.IsConfirmed)
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
                    var nextPositionID = lastTempPositionID++;

                    // copy target page to temp
                    yield return new CheckpointAction
                    {
                        Action = CheckpointActionEnum.CopyToTempFile,
                        PositionID = target.PositionID,
                        TargetPositionID = nextPositionID,
                        MustClear = false
                    };

                    var logIndex = Array.FindIndex(logPositions, x => x.PositionID == target.PositionID);

                    ENSURE(logIndex >= 0);

                    logPositions[logIndex].PhysicalID = nextPositionID;

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
