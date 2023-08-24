namespace LiteDB.Engine;

/// <summary>
/// Page buffer cache. Keep a concurrent dictionary with buffers (byte[]) based on disk file position.
/// Cache both data and log pages. Works at buffer level
/// * Singleton (thread safe)
/// </summary>
[AutoInterface(typeof(IDisposable))]
internal class BufferFactory : IBufferFactory
{
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
    /// Used to create unique identifier to PageBuffer (starts in 1000 just for better debug)
    /// </summary>
    private int _nextUniqueID = 1000;

    public BufferFactory()
    {
    }

    /// <summary>
    /// Allocate, in memory, a new array with PAGE_SIZE inside a PageBuffer struct reference.
    /// </summary>
    public PageBuffer AllocateNewPage(bool isDirty)
    {
        if (_freePages.TryDequeue(out var page))
        {
            _inUsePages.TryAdd(page.UniqueID, page);

            return page;
        }

        Interlocked.Increment(ref _pagesAllocated);

        var uniqueID = Interlocked.Increment(ref _nextUniqueID);

        var newPage = new PageBuffer(uniqueID) { IsDirty = isDirty };

        var added = _inUsePages.TryAdd(newPage.UniqueID, newPage);

        ENSURE(added, new { _pagesAllocated, _freePages, _inUsePages });

        return newPage;
    }

    public void DeallocatePage(PageBuffer page)
    {
        ENSURE(page.ShareCounter == NO_CACHE, "ShareCounter must be 0 before return page to memory", new { page });

        // clear buffer position/sharecounter
        page.Reset();

        // add used page as new free page
        _freePages.Enqueue(page);

        // remove from inUse pages 
        var removed = _inUsePages.TryRemove(page.UniqueID, out _);

        ENSURE(removed, new { page, _pagesAllocated, _freePages, _inUsePages });
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
        return $"InUse: {_inUsePages.Count} - FreeBuffers: {_freePages.Count} - Allocated: {_pagesAllocated}";
    }

    public void Dispose()
    {
        // diattach all PageBuffers from _freePages object
        _freePages.Clear();
    }
}