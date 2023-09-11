using System;
using System.Reflection;
using System.Xml.Linq;

namespace LiteDB.Engine;

public partial class LiteEngine : ILiteEngine
{
    public async Task<int> EnsureIndexAsync(string collectionName, string indexName, BsonExpression expression, bool unique)
    {
        if (_factory.State != EngineState.Open) throw new Exception("must be open");

        // dependency injection
        var autoIdService = _factory.AutoIdService;
        var masterService = _factory.MasterService;
        var monitorService = _factory.MonitorService;
        var collation = _factory.FileHeader.Collation;

        // get current $master
        var master = masterService.GetMaster(false);

        // if collection do not exists, retruns 0
        if (!master.Collections.TryGetValue(collectionName, out var collection)) return 0;

        // create a new transaction locking colID
        var transaction = await monitorService.CreateTransactionAsync(new byte[] { MASTER_COL_ID, collection.ColID });

        var dataService = _factory.CreateDataService(transaction);
        var indexService = _factory.CreateIndexService(transaction);

        // create new index (head/tail)
        var (head, tail) = indexService.CreateHeadTailNodes(collection.ColID);

        // get a free index slot
        var freeIndexSlot = (byte)Enumerable.Range(1, INDEX_MAX_LEVELS)
            .Where(x => collection.Indexes.Any(y => y.Slot == x) == false)
            .FirstOrDefault();

        // create new collection in $master and returns a new master document
        var indexDocument = new IndexDocument()
        {
            Slot = freeIndexSlot,
            Name = indexName,
            Expression = expression,
            Unique = unique,
            HeadIndexNodeID = head,
            TailIndexNodeID = tail
        };
        
        // add new index into master model
        collection.Indexes.Add(indexDocument);
        
        // write master collection into pages inside transaction
        masterService.WriteCollection(master, transaction);

        // create pipe context
        var pipeContext = new PipeContext(dataService, indexService, BsonDocument.Empty);

        var exprInfo = expression.GetInfo();
        var fields = exprInfo.RootFields;

        // get index nodes created
        var counter = 0;

        // getting headerNodeResult (node+page) for new index
        var headResult = indexService.GetNode(indexDocument.HeadIndexNodeID);

        // read all documents based on a full PK scan
        using (var enumerator = new IndexNodeEnumerator(indexService, collection.PK))
        {
            while(enumerator.MoveNext())
            {
                var pkIndexNode = enumerator.Current;
                var dataBlockID = pkIndexNode.DataBlockID;
                var defrag = false;

                // read document fields
                var docResult = dataService.ReadDocument(pkIndexNode.DataBlockID, fields);

                if (docResult.Fail) throw docResult.Exception;

                // get all keys for this index
                var keys = expression.GetIndexKeys(docResult.Value.AsDocument, collation);

                var first = IndexNodeResult.Empty;
                var last = IndexNodeResult.Empty;

                foreach (var key in keys)
                {
                    var node = indexService.AddNode(collection.ColID, indexDocument, key, dataBlockID, headResult, last, out defrag);

                    // ensure execute reload on indexNode after any defrag
                    if (defrag && pkIndexNode.IndexNodeID.PageID == node.IndexNodeID.PageID)
                    {
                        pkIndexNode.Reload();
                    }

                    // keep first node to add in NextNode list (after pk)
                    if (first.IsEmpty) first = node;

                    last = node;
                    counter++;
                }

                ENSURE(first.IsEmpty == false);
                //pkIndexNode.Reload();

                pkIndexNode.NextNodeID = first.IndexNodeID;

                unsafe
                {
                    pkIndexNode.Page->IsDirty = true;
                }

                // do a safepoint after insert each document
                if (monitorService.Safepoint(transaction))
                {
                    await transaction.SafepointAsync();
                    
                    // after safepoint, reload headResult (page pointer changes)
                    headResult = indexService.GetNode(indexDocument.HeadIndexNodeID);
                }
            }
        }

        // write all dirty pages into disk
        await transaction.CommitAsync();

        // release transaction
        monitorService.ReleaseTransaction(transaction);

        // TODO: retornar em formato de array? quem sabe a entrada pode ser um BsonValue (array/document) e o retorno o mesmo
        return counter;
    }
}
