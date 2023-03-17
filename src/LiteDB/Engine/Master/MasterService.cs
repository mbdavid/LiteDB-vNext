namespace LiteDB.Engine;

[AutoInterface(typeof(IDisposable))]
internal class MasterService : IMasterService
{
    private readonly IServicesFactory _factory;
    private readonly IDiskService _disk;

    /// <summary>
    /// All data from master collection are inside this document
    /// </summary>
    private BsonDocument _data;

    /// <summary>
    /// Get collection index (based on collection name) inside _data 
    /// </summary>
    private readonly Dictionary<string, int> _collectionIndex;

    public MasterService(IServicesFactory factory)
    {
        _factory = factory;
        _disk = factory.Disk;

        var collation = _factory.Settings.Collation;

        _data = new BsonDocument
        {
            [MK_COL] = new BsonDocument(),
            [MK_PRAGMAS] = new BsonDocument
            {
                [MK_PRAGMA_USER_VERSION] = 0,
                [MK_PRAGMA_COLLATION] = collation.ToString(),
                [MK_PRAGMA_TIMEOUT] = 60,
                [MK_PRAGMA_CHECKPOINT] = 1000,
                [MK_PRAGMA_LIMIT_SIZE] = 0
            }
        };
    }

    public async Task InitializeAsync()
    {


    }

    public BsonDocument GetCollection(string collectionName)
    {
        if (_collectionIndex.TryGetValue(collectionName, out var index))
        {
            return _data[MK_COL].AsDocument[index].AsDocument;
        }


        return _data[MK_COL];
    }

    public void Dispose()
    {
    }
}
