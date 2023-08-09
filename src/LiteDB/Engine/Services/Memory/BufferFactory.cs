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
    /// A queue of all available (re-used) free page buffers. Rental model
    /// </summary>
    private readonly ConcurrentQueue<PageBuffer> _freePages = new();

    /// <summary>
    /// Track how many pages was allocated in memory. Reduce this size occurs only in CleanUp process
    /// </summary>
    private int _pagesAllocated = 0;

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
            return page;
        }

        Interlocked.Increment(ref _pagesAllocated);

        return new PageBuffer() { IsDirty = isDirty };
    }

    public void DeallocatePage(PageBuffer page)
    {
        ENSURE(() => page.ShareCounter == NO_CACHE, "ShareCounter must be 0 before return page to memory");

        // clear buffer position/sharecounter
        page.Reset();

        // neste momento posso escolher se adiciono no _freePages
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

    public void Dispose()
    {
        // diattach all PageBuffers from _freePages object
        _freePages.Clear();
    }
}