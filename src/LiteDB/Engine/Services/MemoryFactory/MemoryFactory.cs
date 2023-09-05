namespace LiteDB.Engine;

/// <summary>
/// * Singleton (thread safe)
/// </summary>
[AutoInterface(typeof(IDisposable))]
unsafe internal class MemoryFactory : IMemoryFactory
{
    private readonly ConcurrentDictionary<int, nint> _inUsePages = new();
    private readonly ConcurrentQueue<nint> _freePages = new();

    private int _nextUniqueID = BUFFER_UNIQUE_ID - 1;
    private int _pagesAllocated = 0;

    public MemoryFactory()
    {
    }

    public PageMemory* AllocateNewPage()
    {
        if (_freePages.TryDequeue(out var page))
        {
            var pageMemoryPtr = (PageMemory*)page;
            var uniqueID = pageMemoryPtr->UniqueID;

            _inUsePages.TryAdd(uniqueID, page);

            //ENSURE(page.IsCleanInstance, "Page are not clean to be allocated", page);

            return pageMemoryPtr;
        }

        // get memory pointer from unmanaged memory
        var pagePtr = (PageMemory*)Marshal.AllocHGlobal(sizeof(PageMemory));

        Interlocked.Increment(ref _nextUniqueID);
        Interlocked.Increment(ref _pagesAllocated);

        pagePtr->Initialize(_nextUniqueID);

        _inUsePages.TryAdd(pagePtr->UniqueID, (nint)pagePtr);

        return pagePtr;
    }

    public void DeallocatePage(PageMemory* page)
    {
        ENSURE(page->ShareCounter != NO_CACHE, page->ShareCounter != 0, "ShareCounter must be 0 before return page to memory");

        // remove from inUse pages 
        var removed = _inUsePages.TryRemove(page->UniqueID, out _);

        ENSURE(removed, new { _pagesAllocated, _freePages, _inUsePages });

        // clear page
        page->Initialize(page->UniqueID);

        // add used page as new free page
        _freePages.Enqueue((nint)page);
    }


    public override string ToString()
    {
        return Dump.Object(this);
    }

    public void Dispose()
    {
        ENSURE(_pagesAllocated == (_inUsePages.Count + _freePages.Count));

        // release unmanaged memory
        foreach (var ptr in _inUsePages.Values)
        {
            Marshal.FreeHGlobal(ptr);
        }
        foreach (var ptr in _freePages)
        {
            Marshal.FreeHGlobal(ptr);
        }

        _inUsePages.Clear();
        _freePages.Clear();
    }
}
