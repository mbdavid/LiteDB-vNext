using System.Net.Http.Headers;

namespace LiteDB.Engine;

public partial class LiteEngine : ILiteEngine
{
    public Guid Query(string collectionName, Query query, BsonDocument? parameters)
        => this.QueryInternal(collectionName, query, parameters).CursorID;

    public Guid Query(string collectionName, Query query, BsonDocument? parameters, out string explanPlain)
    {
        var cursor = this.QueryInternal(collectionName, query, parameters);

        if (cursor.IsEmpty)
        {
            explanPlain = "";
            return Guid.Empty;
        }

        // get execution plan about this enumerator created by optimezer
        var builder = new ExplainPlainBuilder();

        cursor.Enumerator.GetPlan(builder, 0);

        explanPlain = builder.ToString();

        return cursor.CursorID;
    }

    private Cursor QueryInternal(string collectionName, Query query, BsonDocument? parameters = null)
    {
        var queryService = _factory.QueryService;
        var walIndexService = _factory.WalIndexService;
        var masterService = _factory.MasterService;

        if (_factory.State != EngineState.Open) throw ERR("must be opened");

        // get current $master
        var master = masterService.GetMaster(false);

        // if collection do not exists, return empty
        if (!master.Collections.TryGetValue(collectionName, out var collection))
        {
            return Cursor.Empty;
        }

        // get next read version without open a new transaction
        var readVersion = walIndexService.GetNextReadVersion();

        return queryService.CreateCursor(collection, readVersion, query, parameters ?? BsonDocument.Empty);
    }
}