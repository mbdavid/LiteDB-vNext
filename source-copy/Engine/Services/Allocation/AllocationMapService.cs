namespace LiteDB.Engine;

/// <summary>
/// Represent AllocationMap service to monitor free page size and page extends. Shuld be store in disk before dispose
/// </summary>
internal class AllocationMapService : IDisposable
{
    private readonly List<AllocationMapPage> _pages = new();

    private readonly DiskService _disk;

    private readonly uint[] _colIndexLastPageID = new uint[byte.MaxValue];
    private readonly uint[] _colDataLastPageID = new uint[byte.MaxValue];

    /// <summary>
    /// Read all AllocationMapPages avaiable in disk
    /// </summary>
    public async Task ReadMapPages(CancellationToken cancellationToken = default)
    {
        using var reader = _disk.GetReader();

        long position = AMP_FIRST_PAGE_ID * PAGE_SIZE;

        while(position < _disk.FileLength)
        {
            var buffer = new BufferPage(false);

            await reader.ReadPageAsync(buffer.Memory, position, cancellationToken);

            if (buffer.IsEmpty()) break;

            var mapPage = new AllocationMapPage(buffer);

            _pages.Add(mapPage); // pages are added in index order

            position += (AMP_STEP_SIZE * PAGE_SIZE);
        }
    }

    /// <summary>
    /// Update map using pageID to found each allocation map page/page location must be changed
    /// Tests colID to be the same as extend (if extend colID == 0, set this colID else trow)
    /// Tests pageType to be the same as pageType (if pageType == 0, set this pageType else throw)
    /// </summary>
    public void UpdateMap(uint pageID, PageType pageType, byte colID, ushort freeSpace)
    {
        var pageIndex = (int)((pageID - AMP_FIRST_PAGE_ID) / AMP_STEP_SIZE);
        var page = _pages[pageIndex];

    }

    /// <summary>
    /// Get a existing page for this collection with at least "length" free size. Returns MaxValue if not found (need create new)
    /// </summary>
    public uint GetFreePageID(byte coldID, PageType type, int length)
    {
        // busca uma pagina que contenha o espaço necessário
        // lock
        return 0;
    }

    /// <summary>
    /// Create a new Page in allocation map pages. Create extends/new pages if needed. Return new PageID allocated and if this new page is the LastPageID of datafile
    /// </summary>
    public uint NewPageID(byte colID, PageType type, out bool isLastPageID)
    {
        isLastPageID = false;
        // lock
        return 0;
    }


    /// <summary>
    /// Get a IEnumerable with all changed pages to write on disk in Shutdown process
    /// </summary>
    public IEnumerable<Memory<byte>> GetDirtyPages()
    {
        foreach(var page in _pages)
        {
            if (page.IsDirty)
            {
                yield return page.GetBufferWrite();
            }
        }
    }

    public void Dispose()
    {
        foreach(var page in _pages)
        {
            page.Dispose();
        }
    }
}
