namespace LiteDB.Engine;

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

        // get all log pages and temp pages order by PositionID
        var logPositions = logPages
            .Select(x => new LogPosition
            {
                PositionID = x.PositionID,
                PageID = x.PageID,
                PhysicalID = x.PositionID, // in log pages, physical positionID == position ID
                IsConfirmed = confirmedTransactions.Contains(x.TransactionID)
            })
            .Union(tempPages.Select(t => new LogPosition
            {
                PageID = t.PageID,
                PositionID = t.PositionID,
                PhysicalID = startTempPositionID++, // in temp pages, positionID has diferent physical id
                IsConfirmed = true
            }))
            .OrderBy(x => x.PositionID)
            .Distinct()
            .ToArray();

        // create dict with all duplicates pageID getting last positionID
        var duplicates = logPositions
            .Where(x => x.IsConfirmed)
            .GroupBy(x => x.PageID)
            .Where(x => x.Count() > 1)
            .Select(x => (PageID: x.Key, PositionID: x.Max(y => y.PositionID)))
            .ToDictionary(x => x.PageID, x => x);

        for (var i = 0; i < logPositions.Length; i++)
        {
            var logPage = logPositions[i];

            var willOverride = false;

            // check if this page will be override in future
            if (duplicates.TryGetValue(logPage.PageID, out var lastDuplicate))
            {
                willOverride = logPage.PositionID < lastDuplicate.PositionID;
            }

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
                var targetIndex = Array.FindIndex(logPositions, x => x.PositionID == logPage.PositionID);

                if (targetIndex == -1)
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
                    // get a new position on temp
                    var nextPositionID = ++lastTempPositionID;

                    // copy target page to temp
                    yield return new CheckpointAction
                    {
                        Action = CheckpointActionEnum.CopyToTempFile,
                        PositionID = logPositions[targetIndex].PositionID,
                        TargetPositionID = nextPositionID,
                        MustClear = false
                    };

                    logPositions[targetIndex].PhysicalID = nextPositionID;

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
            else
            {
                throw new NotImplementedException();
            }
        }
    }
}
