namespace LiteDB.Engine;

/// <summary>
/// Page buffer cache. Keep a concurrent dictionary with buffers (byte[]) based on disk file position.
/// Cache both data and log pages. Works at buffer level
/// * Singleton (thread safe)
/// </summary>
[AutoInterface(typeof(IDisposable))]
internal class BufferFactory : IBufferFactory
{
    /// <summary>
    /// All created instances 
    /// </summary>
    private readonly ConcurrentDictionary<int, PageBuffer> _inUsePages = new();

    /// <summary>
    /// A queue of all available (re-used) free page buffers. Rental model
    /// </summary>
    private readonly ConcurrentQueue<PageBuffer> _freePages = new();

    /// <summary>
    /// Track how many pages was allocated in memory. Reduce this size occurs only in CleanUp process
    /// </summary>
    private int _pagesAllocated = 0;

    /// <summary>
    /// Used to create unique identifier to PageBuffer
    /// </summary>
    private int _nextUniqueID = BUFFER_UNIQUE_ID;

    public BufferFactory()
    {
    }

    /// <summary>
    /// Allocate, in memory, a new array with PAGE_SIZE inside a PageBuffer struct reference.
    /// </summary>
    public PageBuffer AllocateNewPage()
    {
        if (_freePages.TryDequeue(out var page))
        {
            _inUsePages.TryAdd(page.UniqueID, page);

            ENSURE(page.IsCleanInstance, "Page are not clean to be allocated", page);

            return page;
        }

        Interlocked.Increment(ref _pagesAllocated);

        var uniqueID = Interlocked.Increment(ref _nextUniqueID);

        var newPage = new PageBuffer(uniqueID);

        var added = _inUsePages.TryAdd(newPage.UniqueID, newPage);

        ENSURE(added, new { _pagesAllocated, _freePages, _inUsePages });

        return newPage;
    }

    /// <summary>
    /// After use page, return to free list to be reused later
    /// </summary>
    public void DeallocatePage(PageBuffer page)
    {
        ENSURE(page.ShareCounter == NO_CACHE, "ShareCounter must be 0 before return page to memory", page);

        // remove from inUse pages 
        var removed = _inUsePages.TryRemove(page.UniqueID, out _);

        ENSURE(removed, new { page, _pagesAllocated, _freePages, _inUsePages });

        // clear buffer position/sharecounter
        page.Reset();

        // add used page as new free page
        _freePages.Enqueue(page);
    }

    public int CleanUp()
    {
        // faz um for e limpa toda _cache que tiver shared = 0 (chama dispose)
        // retorna quantas paginas estão na _cache ainda
        // não precisa de lock exclusivo para rodar
        // faz gum GC
        return 0;
    }

    public override string ToString()
    {
        return Dump.Object(this);
    }

    public void Dispose()
    {
        ENSURE(_inUsePages.Count == 0, this);
        ENSURE(_freePages.Count == _pagesAllocated, this);

        // all others PageBuffer references are done... now, let's remove last one
        _freePages.Clear();
        _inUsePages.Clear();
        _pagesAllocated = 0;
        _nextUniqueID = 100;
    }
}