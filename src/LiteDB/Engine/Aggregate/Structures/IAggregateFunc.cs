namespace LiteDB.Engine;

/// <summary>
/// </summary>
public interface IAggregateFunc
{
    void Iterate(BsonValue key, BsonDocument document);
    BsonValue GetResult();
    void Reset();
}