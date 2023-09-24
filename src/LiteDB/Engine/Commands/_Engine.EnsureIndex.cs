namespace LiteDB.Engine;

public partial class LiteEngine : ILiteEngine
{
    public async Task<int> EnsureIndexAsync(string collectionName, string indexName, BsonExpression expression, bool unique)
    {
        throw new NotImplementedException();
    }
}
