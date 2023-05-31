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
    private readonly IBufferFactory _bufferFactory;
    private readonly IBsonReader _reader;
    private readonly IBsonWriter _writer;

    #region $master document structure

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

    #endregion

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

    public MasterService(IServicesFactory factory)
    {
        _factory = factory;
        _disk = factory.GetDisk();
        _bufferFactory = factory.GetBufferFactory();
        _reader = factory.GetBsonReader();
        _writer = factory.GetBsonWriter();
    }

    #region Read/Write $master

    /// <summary>
    /// Initialize (when database open) reading first extend pages. Database should have no log data to read this
    /// Initialize _master document instance
    /// </summary>
    public async Task InitializeAsync()
    {
        // create a a local transaction (not from monitor)
        using var transaction = new Transaction(_factory, 0, new byte[0], 0);

        // initialize data service with new transaction
        var dataService = new DataService(_factory, transaction);

        // read $master document
        var master = await dataService.ReadDocumentAsync(MASTER_ROW_ID, null);

        // update current instance with new $master loaded
        this.UpdateDocument(master);
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
    /// Write all master document into page buffer and write on this. Must use a real transaction
    /// to store all pages into log
    /// </summary>
    public async Task WriteCollectionAsync(BsonDocument master, ITransaction transaction)
    {
        var dataService = new DataService(_factory, transaction);

        await dataService.UpdateDocumentAsync(MASTER_ROW_ID, master);
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
            [MK_PRAGMA_LIMIT_SIZE] = 0L, // should be long
            [MK_PRAGMA_CHECKPOINT] = CHECKPOINT_SIZE,
        }
    };

    #endregion

    public void Dispose()
    {
    }
}
