namespace LiteDB.Engine;

internal interface IDocumentLookup
{
    ValueTask<BsonDocument> LoadAsync(PageAddress rawId);
}