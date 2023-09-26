namespace LiteDB.Engine;

/// <summary>
/// Document store factory to cache results
/// </summary>
[AutoInterface]
internal class DocumentStoreFactory : IDocumentStoreFactory
{
    private ConcurrentDictionary<string, IDocumentStore> _cache = new(StringComparer.OrdinalIgnoreCase);

    public IDocumentStore GetUserCollection(string name)
    {
        var store = _cache.GetOrAdd(name, (n) => new UserCollectionStore(n));

        return store;
    }

    public IDocumentStore GetVirtualCollection(string name, BsonDocument parameters)
    {
        //TODO: vai conter SWITCH para decidir
        throw new NotImplementedException();
    }
}
