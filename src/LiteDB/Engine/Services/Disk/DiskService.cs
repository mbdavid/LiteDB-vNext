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

        long position = AM_FIRST_PAGE_ID * PAGE_SIZE;

        var fileLength = _writer.GetLength();

        while (position < fileLength)
        {
            var pageBuffer = memoryCache.AllocateNewBuffer();

            await _writer.ReadAsync(position, pageBuffer);

            //if (pageBuffer.IsEmpty()) break;

            yield return pageBuffer;

            position += (AM_PAGE_STEP * PAGE_SIZE);
        }

    }

    private async Task CreateNewDatafileAsync()
    {
        var memoryCache = _factory.MemoryCache;

        var headerBuffer = memoryCache.AllocateNewBuffer();
        var amBuffer = memoryCache.AllocateNewBuffer();

        var headerPage = new HeaderPage(headerBuffer);
        var amPage = new AllocationMapPage(1, amBuffer);

        headerPage.UpdateHeaderBuffer();
        amPage.UpdateHeaderBuffer();

        await _writer.WriteAsync(headerBuffer);
        await _writer.WriteAsync(amBuffer);

        memoryCache.DeallocateBuffer(headerBuffer);
        memoryCache.DeallocateBuffer(amBuffer);
    }

    public void Dispose()
    {
    }
}
