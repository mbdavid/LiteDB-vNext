namespace LiteDB.Engine;

/// <summary>
/// Page buffer cache. Keep a concurrent dictionary with buffers (byte[]) based on disk file position.
/// Cache both data and log pages. Works at buffer level
/// * Singleton (thread safe)
/// </summary>
[AutoInterface(typeof(IDisposable))]
internal class BufferFactory : IBufferFactory
{
    private readonly IMemoryCacheService _memoryCache;

    /// <summary>
    /// A queue of all available (re-used) free page buffers. Rental model
    /// </summary>
    private readonly ConcurrentQueue<PageBuffer> _freePages = new();

    /// <summary>
    /// Track how many pages was allocated in memory. Reduce this size occurs only in CleanUp process
    /// </summary>
    private int _pagesAllocated = 0;

    public BufferFactory(IMemoryCacheService memoryCache)
    {
        _memoryCache = memoryCache;
    }

    /// <summary>
    /// Allocate, in memory, a new array with PAGE_SIZE inside a PageBuffer struct reference.
    /// </summary>
    public PageBuffer AllocateNewPage()
    {
        if (_freePages.TryDequeue(out var page))
        {
            return page;
        }

        Interlocked.Increment(ref _pagesAllocated);

        return new PageBuffer();
    }

    public void DeallocatePage(PageBuffer page)
    {
        ENSURE(page.ShareCounter == 0, "ShareCounter must be 0 before return page to memory");

        // clear buffer position/sharecounter
        page.Reset();

        // neste momento posso escolher se adiciono no _freePages
        _freePages.Enqueue(page);
    }

    /// <summary>
    /// Try convert a readable page buffer into a writable page buffer. If only current thread are using, convert it.
    /// Otherwise return a new AllocateNewPage()
    /// </summary>
    public PageBuffer GetWritePage(PageBuffer readPage)
    {
        // if readBuffer are not used by anyone in cache (ShareCounter == 1 - only current thread), remove it
        if (_memoryCache.TryRemovePageFromCache(readPage, 1))
        {
            // it's safe here to use readBuffer as writeBuffer (nobody else are reading)
            return readPage;
        }
        else
        {
            // create a new page in memory
            var buffer = this.AllocateNewPage();

            // copy content from clean buffer to write buffer (if exists)
            readPage.AsSpan().CopyTo(buffer.AsSpan());

            return buffer;
        }
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
    }
}