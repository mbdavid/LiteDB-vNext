namespace LiteDB.Engine;

internal class UserCollectionStore : IDocumentStore
{
    public string Name { get; }
    public byte ColID { get; }
    public IReadOnlyList<IndexDocument> Indexes { get; }

    private CollectionDocument? _collection;

    public UserCollectionStore(string name)
    {
        this.Name = name;
    }

    public void Load(IMasterService masterService)
    {
        var master = masterService.GetMaster(false);

        if (master.Collections.TryGetValue(this.Name, out var collection))
        {
            _collection = collection;
        }

        throw ERR($"Collection {this.Name} do not exists");
    }

    public IPipeEnumerator GetPipeEnumerator(BsonExpression expression)
    {
        throw new NotImplementedException();
    }

    public IReadOnlyList<IndexDocument> GetIndexes() => 
        _collection!.Indexes;

    public (IDataService dataService, IIndexService indexService) GetServices(IServicesFactory factory, ITransaction transaction) =>
        (factory.CreateDataService(transaction), factory.CreateIndexService(transaction));

    public void Dispose()
    {
    }
}
