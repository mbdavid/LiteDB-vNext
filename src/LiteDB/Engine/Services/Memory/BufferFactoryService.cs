namespace LiteDB.Engine;

/// <summary>
/// Page buffer cache. Keep a concurrent dictionary with buffers (byte[]) based on disk file position.
/// Cache both data and log pages. Works at buffer level
/// * Singleton (thread safe)
/// </summary>
[AutoInterface(typeof(IDisposable))]
internal class BufferFactoryService : IBufferFactoryService
{
    /// <summary>
    /// A queue of all available (re-used) free buffers. Rent model
    /// </summary>
    private readonly ConcurrentQueue<PageBuffer> _freeBuffers = new();

    /// <summary>
    /// Track how many pages was allocated in memory. Reduce this size occurs only in CleanUp process
    /// </summary>
    private int _pagesAllocated = 0;

    public BufferFactoryService()
    {
    }

    /// <summary>
    /// Allocate, in memory, a new array with PAGE_SIZE inside a PageBuffer struct reference.
    /// </summary>
    public PageBuffer AllocateNewBuffer()
    {
        if (_freeBuffers.TryDequeue(out var page))
        {
            return page;
        }

        var array = new byte[PAGE_SIZE];

        Interlocked.Increment(ref _pagesAllocated);

        return new PageBuffer(array);
    }

    public void DeallocateBuffer(PageBuffer buffer)
    {
        ENSURE(buffer.ShareCounter == 0, "ShareCounter must be 0 before return page to memory");

        // clear buffer position/sharecounter
        buffer.Reset();

        // neste momento posso escolher se adiciono no _freePages
        _freeBuffers.Enqueue(buffer);
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