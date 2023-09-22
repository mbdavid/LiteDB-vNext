using System.Xml.Linq;

namespace LiteDB.Engine;

internal class UserCollectionStore : IDocumentStore
{
    private readonly string _name;

    public byte ColID => _collection?.ColID ?? 0;
    public string Name => _name;
    public IReadOnlyList<IndexDocument> Indexes => _collection?.Indexes ?? (IReadOnlyList<IndexDocument>)Array.Empty<IndexDocument>();

    private CollectionDocument? _collection;

    public UserCollectionStore(string name)
    {
        _name = name;
    }

    public void Initialize(IMasterService masterService)
    {
        var master = masterService.GetMaster(false);

        if (master.Collections.TryGetValue(_name, out var collection))
        {
            _collection = collection;
        }
        else
        {
            throw ERR($"Collection {_name} do not exists");
        }
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
