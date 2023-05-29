namespace LiteDB.Engine;

/// <summary>
/// * Singleton (thread safe)
/// </summary>
[AutoInterface(typeof(IDisposable))]
internal class MasterService : IMasterService
{
    // dependency injection
    private readonly IDiskService _disk;
    private readonly IBufferFactory _bufferFactory;
    private readonly IBsonReader _reader;
    private readonly IBsonWriter _writer;

    /*
    # Master Document Structure
    {
        "collections": {
            "<col-name>": {
                "colID": 1,
                "meta": { ... },
                "indexes": {
                    "<index-name>": {
                        "slot": 0,
                        "expr": "$._id",
                        "unique": true,
                        "headPageID": 8,
                        "headIndex": 0,
                        "tailPageID": 8,
                        "tailIndex": 1,
                        "meta": { ... }
                    },
                    //...
                }
            },
            //...
        },
        "pragmas": {
            "user_version": 0,
            "limit": 0,
            "checkpoint": 1000
        }
    }
    */

    /// <summary>
    /// A dictionary with all collection indexed by collection name
    /// </summary>
    public IDictionary<string, CollectionDocument>? Collections { get; private set; }

    /// <summary>
    /// Get current database pragma values
    /// </summary>
    public PragmaDocument? Pragmas { get; private set; }

    /// <summary>
    /// $master document read from disk
    /// </summary>
    private BsonDocument? _master;

    public MasterService(
        IDiskService disk,
        IBufferFactory bufferFactory,
        IBsonReader reader,
        IBsonWriter writer)
    {
        _disk = disk;
        _bufferFactory = bufferFactory;
        _reader = reader;
        _writer = writer;
    }

    #region Read/Write $master

    /// <summary>
    /// Initialize (when database open) reading first extend pages. Database should have no log data to read this
    /// Initialize _master document instance
    /// </summary>
    public async Task InitializeAsync()
    {
        var page = _bufferFactory.AllocateNewPage(false);
        byte[]? bufferDocument = null;

        var reader = await _disk.RentDiskReaderAsync();

        try
        {
            var pagePositionID = (uint)MASTER_PAGE_ID;

            // get first $master page - first 8k
            await reader.ReadPageAsync(pagePositionID, page);

            // read document size
            var masterLength = page.AsSpan(PAGE_HEADER_SIZE).ReadVariantLength(out _);

            // if $master document fit in one page, just read from buffer
            if (masterLength < PAGE_CONTENT_SIZE)
            {
                var master = _reader.ReadDocument(page.AsSpan(PAGE_HEADER_SIZE), null, false, out _)!;

                this.UpdateDocument(master);
            }
            // otherwise, read as many pages as needed to read full master collection
            else
            {
                // create, in memory, a new array with full size of document
                bufferDocument = ArrayPool<byte>.Shared.Rent(masterLength);

                // copy first page already read
                Buffer.BlockCopy(page.Buffer, 0, bufferDocument, 0, PAGE_HEADER_SIZE);

                // track how many bytes are readed (remove first page already read)
                var bytesToRead = masterLength - PAGE_CONTENT_SIZE;
                var docPosition = PAGE_CONTENT_SIZE;

                // read all document from multiples buffer segments
                while(bytesToRead > 0)
                {
                    // move to another page
                    pagePositionID++;

                    // reuse same buffer (will be copied to bufferDocumnet)
                    await reader.ReadPageAsync(pagePositionID, page);

                    var pageContentSize = bytesToRead > PAGE_CONTENT_SIZE ? PAGE_CONTENT_SIZE : bytesToRead;

                    Buffer.BlockCopy(page.Buffer, 0, bufferDocument, docPosition, pageContentSize);

                    bytesToRead -= pageContentSize;
                    docPosition += pageContentSize;
                }

                // read $master document from full buffer document
                var master = _reader.ReadDocument(bufferDocument, null, false, out _)!;

                this.UpdateDocument(master);
            }
        }
        finally
        {
            // return array to pool to be re-used
            if (bufferDocument is not null)  ArrayPool<byte>.Shared.Return(bufferDocument);

            // deallocate buffer
            _bufferFactory.DeallocatePage(page);

            // return reader disk
            _disk.ReturnDiskReader(reader);
        }
    }

    /// <summary>
    /// Load external master document and update Collection/Pragmas fields
    /// </summary>
    public void UpdateDocument(BsonDocument master)
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
    /// Write all master document into page buffer and write on this.
    /// </summary>
    public async Task WriteCollectionAsync(BsonDocument master, ITransaction transaction)
    {
        // get master length to know if fits in 1 page or need many
        var masterLength = master.GetBytesCount();
        var initialMasterPageID = 1u;
        var page = await transaction.GetPageAsync(initialMasterPageID, true);

        // setup first page header
        page.Header.ColID = 0;
        page.Header.ItemsCount = 1;
        page.Header.PageType = PageType.Data;
        page.Header.PageID = initialMasterPageID;

        // if fits in 1 page
        if (masterLength <= PAGE_CONTENT_SIZE)
        {
            page.Header.UsedBytes = (ushort)masterLength;

            // serialize master document into page content area
            _writer.WriteDocument(page.AsSpan(PAGE_HEADER_SIZE, PAGE_CONTENT_SIZE), master, out _);
        }
        else
        {
            //TODO: slitar em um array alugado e salvar nas paginas
            throw new NotImplementedException();
        }
    }

    #endregion

    #region Operations in $master

    public byte NewColID()
    {
        //TODO: testar limites

        var colID = Enumerable.Range(1, 250)
            .Where(x => this.Collections!.Values.Any(y => y.ColID == x) == false)
            .FirstOrDefault();

        return (byte)colID;
    }

    public BsonDocument AddCollection(byte colID, string name, PageAddress head, PageAddress tail)
    {
        //TODO: validar
        var master = new BsonDocument(_master!);

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

        var slot = Enumerable.Range(1, byte.MaxValue)
            .Where(x => collection.Indexes.Values.Any(y => y.Slot == x) == false)
            .FirstOrDefault();

        if (slot == byte.MaxValue) throw ERR($"{collection.Name} collection has reached the limit of {byte.MaxValue} indexes.");

        var indexes = master[MK_COL].AsDocument
            [collection.Name].AsDocument
            [MK_INDEX].AsDocument;

        indexes[indexName] = this.CreateIndex((byte)slot, expr, unique, head, tail);

        return master;
    }

    private BsonDocument CreateIndex(byte slot, string expr, bool unique, PageAddress head, PageAddress tail) => new()
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

    public BsonDocument DropIndex(byte colID, byte slot)
    {
        var master = new BsonDocument(_master!);
        return master;
    }

    public BsonDocument DropCollection(byte colID)
    {
        var master = new BsonDocument(_master!);
        return master;
    }

    public BsonDocument SetPragma(string pragma, BsonValue value)
    {
        var master = new BsonDocument(_master!);
        return master;
    }

    #endregion

    #region Create new $master

    /// <summary>
    /// Create a new $master document with initial pragma values
    /// </summary>
    public static BsonDocument CreateNewMaster() => new()
    {
        [MK_COL] = new BsonDocument(),
        [MK_PRAGMA] = new BsonDocument
        {
            [MK_PRAGMA_USER_VERSION] = 0,
            [MK_PRAGMA_LIMIT_SIZE] = 0,
            [MK_PRAGMA_CHECKPOINT] = CHECKPOINT_SIZE,
        }
    };

    #endregion

    public void Dispose()
    {
    }
}
