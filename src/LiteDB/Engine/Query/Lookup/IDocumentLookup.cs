namespace LiteDB.Engine;

internal interface IDocumentLookup
{
    ValueTask<BsonDocument> LoadAsync(IndexNode indexNode, IDataService dataService);
    ValueTask<BsonDocument> LoadAsync(PageAddress dataBlock, IDataService dataService);
}