using System;
using System.Collections.Generic;
using System.Transactions;

namespace LiteDB.Engine;

/// <summary>
/// Do all WAL index services based on LOG file - has only single instance per engine
/// * Singleton (thread safe)
/// </summary>
[AutoInterface]
internal class WalIndexService : IWalIndexService
{
    // dependency injection

    /// <summary>
    /// A indexed dictionary by PageID where each item are a sorter-list of read version and disk log position
    /// </summary>
    private readonly ConcurrentDictionary<uint, List<(int version, long position)>> _index = new();

    public WalIndexService()
    {
    }

    /// <summary>
    /// Get a page position (in disk) for a page that are inside WAL. 
    /// Returns MaxValue if not found
    /// </summary>
    public long GetPagePosition(uint pageID, int version, out int walVersion)
    {
        // initial value
        walVersion = 0;

        // if version is 0 or there is no page on log index, return current position on disk
        if (version == 0 || 
            _index.TryGetValue(pageID, out var listVersion) == false)
        {
            return PageService.GetPagePosition(pageID);
        }

        // list are sorted by version number
        var idx = listVersion.Count;
        var position = PageService.GetPagePosition(pageID); // not found (get from data)

        // get all page versions in wal-index
        // and then filter only equals-or-less then selected version
        while (idx > 0)
        {
            idx--;

            var (ver, pos) = listVersion[idx];

            if (ver <= version)
            {
                walVersion = ver;
                position = pos;

                break;
            }
        }

        return position;
    }

    public void AddVersion(int version, IEnumerable<(uint pageID, long position)> pagePositions)
    {
        foreach (var (pageID, position) in pagePositions)
        {
            if (_index.TryGetValue(pageID, out var listVersion))
            {
                // add version/position into pageID
                listVersion.Add(new(version, position));
            }
            else
            {
                listVersion = new()
                {
                    // add version/position into pageID
                    new(version, position)
                };

                // add listVersion with first item in index for this pageID
                _index.TryAdd(pageID, listVersion);
            }
        }
    }

    public void Clear()
    {
        _index.Clear();
    }
}