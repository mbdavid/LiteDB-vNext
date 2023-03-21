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
    private readonly IServicesFactory _factory;

    /// <summary>
    /// A indexed dictionary by PageID where each item are a sorter-list of read version and disk log position
    /// </summary>
    private readonly ConcurrentDictionary<uint, List<(int Version, long Position)>> _index = new();

    public WalIndexService(IServicesFactory factory)
    {
        _factory = factory;
    }

    /// <summary>
    /// Get a page position (in disk) for a page that are inside WAL. 
    /// Returns MaxValue if not found
    /// </summary>
    public long GetPagePosition(uint pageID, int version, out int walVersion)
    {
        // initial value
        walVersion = 0;

        // wal-index versions must be greater than 0 (version 0 is datafile)
        // get (if exists) listVersion of a PageID
        if (version == 0 || 
            _index.TryGetValue(pageID, out var listVersion) == false)
        {
            return long.MaxValue;
        }

        // list are sorted by version number
        var idx = listVersion.Count;
        var position = long.MaxValue; // not found

        // get all page versions in wal-index
        // and then filter only equals-or-less then selected version
        while (idx > 0)
        {
            idx--;

            var item = listVersion[idx];

            if (item.Version <= version)
            {
                walVersion = item.Version;
                position = item.Position;

                break;
            }
        }

        return position;
    }

    public void AddVersion(int version, IEnumerable<(uint PageID, long Position)> pagePositions)
    {
        foreach (var item in pagePositions)
        {
            if (_index.TryGetValue(item.PageID, out var listVersion))
            {
                // add version/position into pageID
                listVersion.Add(new(version, item.Position));
            }
            else
            {
                listVersion = new()
                {
                    // add version/position into pageID
                    new(version, item.Position)
                };

                // add listVersion with first item in index for this pageID
                _index.TryAdd(item.PageID, listVersion);
            }
        }
    }

    public void Clear()
    {
        _index.Clear();
    }
}