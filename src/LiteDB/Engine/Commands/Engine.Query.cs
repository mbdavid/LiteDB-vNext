namespace LiteDB.Engine;

public partial class LiteEngine : ILiteEngine
{
    public Guid? Query(string collectionName, Query query, int fetchCount)
    {
        var masterService = _factory.MasterService;

        if (_factory.State != EngineState.Open) throw ERR("must be opened");

        // get current $master
        var master = masterService.GetMaster(false);

        // if collection do not exists, return null
        if (!master.Collections.TryGetValue(collectionName, out var collection)) return null;

        // var queryDef = queryOpmitization.Optimize(collection, query) // só usa a $master, é sincrona

        // var cursorId = _factory.CreateCursor(queryDef, fetchCount);


        return Guid.NewGuid();
    }
}