namespace LiteDB.Engine;

public partial class LiteEngine : ILiteEngine
{
    public Guid Query(string collectionName, IQuery query, BsonDocument? parameters, out string executionPlan)
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
            executionPlan = "";
            return Guid.Empty;
        }

        // get next read version without open a new transaction
        var readVersion = walIndexService.GetNextReadVersion();

        var cursor = queryService.CreateCursor(collection, readVersion, query, parameters ?? BsonDocument.Empty);

        //TODO: remover quando não for solicitado executionPlan... tem um custo ali

        // get execution plan about this enumerator created by optimezer
        var builder = new ExplainPlainBuilder();

        cursor.Enumerator.GetPlan(builder, 0);

        executionPlan = builder.ToString();

        return cursor.CursorID;
    }
}