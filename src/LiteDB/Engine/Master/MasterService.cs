namespace LiteDB.Engine;

[AutoInterface(typeof(IDisposable))]
internal class MasterService : IMasterService
{
    private readonly IServicesFactory _factory;
    private readonly IDiskService _disk;
    private readonly IMemoryCacheService _memoryCache;
    private readonly IBsonReader _reader;
    private readonly IBsonWriter _writer;

    /// <summary>
    /// All data from master collection are inside this document
    /// </summary>
    private MasterDocument _master;

    public MasterService(IServicesFactory factory)
    {
        _factory = factory;
        _disk = factory.Disk;
        _memoryCache = factory.MemoryCache;
        _reader = factory.BsonReader;
        _writer = factory.BsonWriter;

        var collation = _factory.Settings.Collation;

        // initialize empty $master document
        _master = new MasterDocument(collation);
    }

    /// <summary>
    /// Initialize (when database open) reading first extend pages. Database should have no log data to read this
    /// </summary>
    public async Task InitializeAsync()
    {
        var reader = _disk.RentDiskReader();
        var buffer = _memoryCache.AllocateNewBuffer();
        byte[]? bufferDocument = null;

        try
        {
            // get first $master page
            var pagePosition = BasePage.GetPagePosition(MASTER_PAGE_ID);

            // read first 8k
            await reader.ReadAsync(pagePosition, buffer);

            // read document size
            var masterLength = buffer.AsSpan(PAGE_HEADER_SIZE).ReadVariantLength(out _);

            // if $master document fit in one page, just read from buffer
            if (masterLength < PAGE_CONTENT_SIZE)
            {
                var doc = _reader.ReadDocument(buffer.AsSpan(PAGE_HEADER_SIZE), null, false, out _)!;

                // initialize $master with BsonDocument
                _master = new MasterDocument(doc);
            }
            // otherwise, read as many pages as needed to read full master collection
            else
            {
                // create, in memory, a new array with full size of document
                bufferDocument = ArrayPool<byte>.Shared.Rent(masterLength);

                // copy first page already read
                Buffer.BlockCopy(buffer.Array, 0, bufferDocument, 0, PAGE_HEADER_SIZE);

                // track how many bytes are readed (remove first page already read)
                var bytesToRead = masterLength - PAGE_CONTENT_SIZE;
                var docPosition = PAGE_CONTENT_SIZE;

                // read all document from multiples buffer segments
                while(bytesToRead > 0)
                {
                    // move to another page
                    pagePosition += PAGE_SIZE;

                    // reuse same buffer (will be copied to bufferDocumnet)
                    await reader.ReadAsync(pagePosition, buffer);

                    var pageContentSize = bytesToRead > PAGE_CONTENT_SIZE ? PAGE_CONTENT_SIZE : bytesToRead;

                    Buffer.BlockCopy(buffer.Array, 0, bufferDocument, docPosition, pageContentSize);

                    bytesToRead -= pageContentSize;
                    docPosition += pageContentSize;
                }

                // read $master document from full buffer document
                var doc = _reader.ReadDocument(bufferDocument, null, false, out _)!;

                _master = new MasterDocument(doc);
            }
        }
        finally
        {
            // return array to pool to be re-used
            if (bufferDocument is not null)  ArrayPool<byte>.Shared.Return(bufferDocument);

            // deallocate buffer
            _memoryCache.DeallocateBuffer(buffer);

            // return reader disk
            _disk.ReturnDiskReader(reader);
        }
    }

    /// <summary>
    /// Get collection reference from $master based on name. Returns null if not found
    /// </summary>
    public CollectionDocument? GetCollection(string name)
    {
        if (_master.Collections.TryGetValue(name, out CollectionDocument collection))
        {
            return collection;
        }

        return null;
    }


    public void Dispose()
    {
    }
}
