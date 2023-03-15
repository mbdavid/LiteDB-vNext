using System;

namespace LiteDB.Engine;

/// <summary>
/// Singleton (thread safe)
/// </summary>
[AutoInterface(typeof(IDisposable))]
internal class DiskService : IDiskService
{
    private IServicesFactory _factory;

    private IDiskStream _writer;
    private ConcurrentQueue<IDiskStream> _readers = new ();

    public DiskService(IServicesFactory factory)
    {
        _factory = factory;

        _writer = _factory.CreateDiskStream(true);
    }

    public async Task<bool> InitializeAsync()
    {
        if (_writer.Exists() == false)
        {
            await this.CreateNewDatafileAsync();
        }

        // abre o arquivo e retorna true se está tudo ok e não precisa de recovery

        return true;
    }

    public async IAsyncEnumerable<PageBuffer> ReadAllocationMapPages()
    {
        var memoryCache = _factory.MemoryCache;

        long position = AMP_FIRST_PAGE_ID * PAGE_SIZE;

        var fileLength = _writer.GetLength();

        while (position < fileLength)
        {
            var pageBuffer = memoryCache.AllocateNewPage();

            await _writer.ReadAsync(position, pageBuffer);

            //if (pageBuffer.IsEmpty()) break;

            yield return pageBuffer;

            position += (AMP_STEP_SIZE * PAGE_SIZE);
        }

    }

    private async Task CreateNewDatafileAsync()
    {
        var memoryCache = _factory.MemoryCache;

        var headerBuffer = memoryCache.AllocateNewPage();
        var ampBuffer = memoryCache.AllocateNewPage();

        var headerPage = new HeaderPage(headerBuffer);
        var ampPage = new AllocationMapPage(1, ampBuffer);

        headerPage.UpdateHeaderBuffer();
        ampPage.UpdateHeaderBuffer();

        await _writer.WriteAsync(headerBuffer);
        await _writer.WriteAsync(ampBuffer);

        memoryCache.DeallocatePage(headerBuffer);
        memoryCache.DeallocatePage(ampBuffer);
    }

    public void Dispose()
    {
    }
}
