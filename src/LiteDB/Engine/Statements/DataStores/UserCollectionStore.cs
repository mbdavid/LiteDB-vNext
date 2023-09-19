namespace LiteDB.Engine;

internal class UserCollectionStore : ISourceStore
{
    public string Name { get; }

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
}
