namespace LiteDB.Engine;

[AutoInterface]
internal class PageWriteFactoryService : IPageWriteFactoryService
{
    private readonly IServicesFactory _factory;
    private readonly IBufferFactoryService _bufferFactory;
    private readonly IMemoryCacheService _memoryCache;

    public PageWriteFactoryService(IServicesFactory factory)
    {
        _factory = factory;
        _bufferFactory = factory.GetBufferFactory();
        _memoryCache = factory.GetMemoryCache();
    }

    public PageBuffer GetWriteBuffer(PageBuffer readBuffer)
    {
        // if readBuffer are not used by anyone in cache (ShareCounter == 1 - only current thread), remove it
        if (_memoryCache.TryRemovePageFromCache(readBuffer, 1))
        {
            // it's safe here to use readBuffer as writeBuffer (nobody else are reading)
            return readBuffer;
        }
        else
        {
            // create a new page in memory
            var buffer = _bufferFactory.AllocateNewBuffer();

            // copy content from clean buffer to write buffer (if exists)
            readBuffer.AsSpan().CopyTo(buffer.AsSpan());

            return buffer;
        }
    }
}
