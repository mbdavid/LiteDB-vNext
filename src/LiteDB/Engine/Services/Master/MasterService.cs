using System.Xml.Linq;

namespace LiteDB.Engine;

/// <summary>
/// * Singleton (thread safe)
/// </summary>
[AutoInterface(typeof(IDisposable))]
internal class MasterService : IMasterService
{
    // dependency injection
    private readonly IServicesFactory _factory;
    private readonly IDiskService _disk;
    private readonly IBufferFactoryService _bufferFactory;
    private readonly IBsonReader _reader;
    private readonly IBsonWriter _writer;

    /// <summary>
    /// A dictionary with all collection indexed by collection name
    /// </summary>
    public IDictionary<string, CollectionDocument>? Collections { get; private set; }

    /// <summary>
    /// Get current database pragma values
    /// </summary>
    public PragmaDocument? Pragmas { get; private set; }

    /// <summary>
    /// Master document read from disk
    /// </summary>
    private BsonDocument? _master;

    public MasterService(IServicesFactory factory)
    {
        _factory = factory;
        _disk = factory.GetDisk();
        _bufferFactory = factory.GetBufferFactory();
        _reader = factory.GetBsonReader();
        _writer = factory.GetBsonWriter();

        var collation = _factory.Settings.Collation;
    }

    /// <summary>
    /// Initialize (when database open) reading first extend pages. Database should have no log data to read this
    /// </summary>
    public async Task ReadMasterFromDiskAsync()
    {
        var reader = _disk.RentDiskReader();
        var buffer = _bufferFactory.AllocateNewBuffer();
        byte[]? bufferDocument = null;

        try
        {
            // get first $master page
            var pagePosition = BasePage.GetPagePosition(MASTER_PAGE_ID);

            // read first 8k
            await reader.ReadPageAsync(pagePosition, buffer);

            // read document size
            var masterLength = buffer.AsSpan(PAGE_HEADER_SIZE).ReadVariantLength(out _);

            // if $master document fit in one page, just read from buffer
            if (masterLength < PAGE_CONTENT_SIZE)
            {
                var master = _reader.ReadDocument(buffer.AsSpan(PAGE_HEADER_SIZE), null, false, out _)!;

                this.LoadMasterDocument(master);
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
                    await reader.ReadPageAsync(pagePosition, buffer);

                    var pageContentSize = bytesToRead > PAGE_CONTENT_SIZE ? PAGE_CONTENT_SIZE : bytesToRead;

                    Buffer.BlockCopy(buffer.Array, 0, bufferDocument, docPosition, pageContentSize);

                    bytesToRead -= pageContentSize;
                    docPosition += pageContentSize;
                }

                // read $master document from full buffer document
                var master = _reader.ReadDocument(bufferDocument, null, false, out _)!;

                this.LoadMasterDocument(master);
            }
        }
        finally
        {
            // return array to pool to be re-used
            if (bufferDocument is not null)  ArrayPool<byte>.Shared.Return(bufferDocument);

            // deallocate buffer
            _bufferFactory.DeallocateBuffer(buffer);

            // return reader disk
            _disk.ReturnDiskReader(reader);
        }
    }

    /// <summary>
    /// Load external master document and update Collection/Pragmas fields
    /// </summary>
    private void LoadMasterDocument(BsonDocument master)
    {
        // initialize collection dict
        this.Collections = new Dictionary<string, CollectionDocument>(byte.MaxValue, StringComparer.OrdinalIgnoreCase);

        // get all collections as colName as keys
        var colDocs = master[MK_COL].AsDocument;

        foreach (var colName in colDocs.Keys)
        {
            var colDoc = colDocs[colName].AsDocument;

            var col = new CollectionDocument(colName, colDoc);

            this.Collections[col.Name] = col;
        }

        // load pragma info from document
        this.Pragmas = new PragmaDocument(master[MK_PRAGMA].AsDocument);

        // update master document instance
        _master = master;
    }

    /// <summary>
    /// Write all master document into page buffer. Must send as pages as needed according with document size/page content size
    /// </summary>
    public void WriteMasterBuffer(BsonDocument master, PageBuffer[] pages)
    {
        if (pages.Length == 1)
        {
            var span = pages[0].AsSpan(PAGE_HEADER_SIZE, PAGE_CONTENT_SIZE);

            _writer.WriteDocument(span, master, out _);
        }
        else
        {
            throw new NotImplementedException();
        }
    }

    public BsonDocument AddCollection(string name, PageAddress head, PageAddress tail)
    {
        //TODO: validar
        var master = new BsonDocument(_master!);

        var colID = Enumerable.Range(1, 250)
            .Where(x => this.Collections!.Values.Any(y => y.ColID == x) == false)
            .FirstOrDefault();

        master[MK_COL].AsDocument[name] = new BsonDocument
        {
            [MK_COL_ID] = colID,
            [MK_INDEX] = new BsonDocument
            {
                ["_id"] = this.CreateIndex(0, "$._id", true, head, tail)
            },
            [MK_META] = new BsonDocument()
        };

        return master;
    }

    public BsonDocument AddIndex(CollectionDocument collection, string indexName, string expr, bool unique, PageAddress head, PageAddress tail)
    {
        //TODO: validar
        var master = new BsonDocument(_master!);

        var slot = Enumerable.Range(1, 32)
            .Where(x => collection.Indexes.Values.Any(y => y.Slot == x) == false)
            .FirstOrDefault();

        var indexes = master[MK_COL].AsDocument
            [collection.Name].AsDocument
            [MK_INDEX].AsDocument;

        indexes[indexName] = this.CreateIndex((byte)slot, expr, unique, head, tail);

        return master;
    }

    private BsonDocument CreateIndex(byte slot, string expr, bool unique, PageAddress head, PageAddress tail)
    {
        return new BsonDocument
        {
            [MK_IDX_SLOT] = slot,
            [MK_IDX_EXPR] = expr,
            [MK_IDX_UNIQUE] = unique,
            [MK_IDX_HEAD_PAGE_ID] = head.PageID,
            [MK_IDX_HEAD_INDEX] = head.Index,
            [MK_IDX_TAIL_PAGE_ID] = tail.PageID,
            [MK_IDX_TAIL_INDEX] = tail.Index,
            [MK_META] = new BsonDocument()
        };
    }

    public BsonDocument DropIndex(byte colID, byte slot)
    {
        var clone = new BsonDocument(_master!);
        return clone;
    }

    public BsonDocument DropCollection(byte colID)
    {
        var clone = new BsonDocument(_master!);
        return clone;
    }

    public BsonDocument SetPragma(string pragma, BsonValue value)
    {
        var clone = new BsonDocument(_master!);
        return clone;
    }

    public void Dispose()
    {
    }
}
