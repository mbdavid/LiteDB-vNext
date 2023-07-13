namespace LiteDB.Engine;

public partial class LiteEngine : ILiteEngine
{
    public Guid Query(string collectionName, IQuery query, BsonDocument? parameters = null)
    {
        var queryService = _factory.QueryService;
        var walIndexService = _factory.WalIndexService;
        var masterService = _factory.MasterService;

        if (_factory.State != EngineState.Open) throw ERR("must be opened");

        // get current $master
        var master = masterService.GetMaster(false);

        // if collection do not exists, return empty
        if (!master.Collections.TryGetValue(collectionName, out var collection)) return Guid.Empty;

        // get next read version without open a new transaction
        var readVersion = walIndexService.GetNextReadVersion();

        var cursor = queryService.CreateCursor(collection, readVersion, query, parameters ?? BsonDocument.Empty);

        return cursor.CursorID;
    }
}