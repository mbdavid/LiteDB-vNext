namespace LiteDB.Engine;

public partial class LiteEngine : ILiteEngine
{
    public async Task<bool> CreateCollectionAsync(string collectionName)
    {
        if (_factory.State != EngineState.Open) throw ERR("must be open");

        var masterService = _factory.GetMaster();
        var monitorService = _factory.GetMonitor();

        // create a new transaction locking colID = 255 ($master)
        var transaction = await monitorService.CreateTransactionAsync(new byte[] { MASTER_COL_ID });

        // get a new colID
        var colID = masterService.NewColID();

        // get index service
        var indexer = _factory.CreateIndexService(transaction);

        // insert head/tail nodes
        var pkNodes = await indexer.CreateHeadTailNodesAsync(colID);

        // create new collection in $master and returns a new master document
        var master = masterService.AddCollection(colID, collectionName, pkNodes.head.RowID, pkNodes.tail.RowID);

        // write master collection into first 8 pages 
        await masterService.WriteCollectionAsync(master, transaction);

        // write all dirty pages into disk
        await transaction.CommitAsync();

        // update master document
        masterService.UpdateDocument(master);

        // release transaction
        monitorService.ReleaseTransaction(transaction);

        return true;
    }
}
