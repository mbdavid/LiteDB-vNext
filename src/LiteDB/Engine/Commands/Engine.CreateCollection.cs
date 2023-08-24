namespace LiteDB.Engine;

public partial class LiteEngine : ILiteEngine
{
    public async Task<bool> CreateCollectionAsync(string collectionName)
    {
        if (_factory.State != EngineState.Open) throw ERR("must be open");

        // dependency inejctions
        var masterService = _factory.MasterService;
        var monitorService = _factory.MonitorService;

        // get exclusive $master
        var master = masterService.GetMaster(true);

        // get a new colID
        var colID = (byte)Enumerable.Range(1, MASTER_COL_LIMIT + 1)
            .Where(x => master.Collections.Values.Any(y => y.ColID == x) == false)
            .FirstOrDefault();

        if (colID > MASTER_COL_LIMIT) throw ERR("acima do limite");

        // create a new transaction locking colID = 255 ($master)
        var transaction = await monitorService.CreateTransactionAsync(new byte[] { MASTER_COL_ID, colID });

        // get index service
        var indexer = _factory.CreateIndexService(transaction);

        // insert head/tail nodes
        var (head, tail) = await indexer.CreateHeadTailNodesAsync(colID);

        // create new collection in $master and returns a new master document
        master.Collections.Add(collectionName, new CollectionDocument()
        {
            ColID = colID,
            Name = collectionName,
            Indexes = new Dictionary<string, IndexDocument>(StringComparer.OrdinalIgnoreCase)
            {
                ["_id"] = new IndexDocument
                {
                    Slot = 0,
                    Name = "_id",
                    Expression = "$._id",
                    Unique = true,
                    HeadIndexNodeID = head.IndexNodeID,
                    TailIndexNodeID = tail.IndexNodeID
                }
            }
        });

        // write master collection into pages
        await masterService.WriteCollectionAsync(master, transaction);

        // write all dirty pages into disk
        await transaction.CommitAsync();

        // update master document (only after commit completed)
        masterService.SetMaster(master);

        // release transaction
        monitorService.ReleaseTransaction(transaction);

        return true;
    }
}
