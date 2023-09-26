namespace LiteDB.Engine;

internal struct Into : IIsEmpty
{
    public readonly static Into Empty = new();

    private readonly IDocumentStore? _store = default;
    private readonly BsonAutoId _autoId = BsonAutoId.Int32;

    public Into(IDocumentStore store, BsonAutoId autoId)
    {
        _store = store;
        _autoId = autoId;
    }

    public bool IsEmpty => _store is null;

    public IDocumentStore Store => _store!;
    public BsonAutoId AutoId => _autoId;

}
