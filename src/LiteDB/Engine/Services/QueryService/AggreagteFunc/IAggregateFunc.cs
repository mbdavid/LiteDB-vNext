namespace LiteDB.Engine;

/// <summary>
/// </summary>
public interface IAggregateFunc
{
    string Name { get; }
    BsonExpression Expression { get; }
    void Iterate(BsonValue key, BsonDocument document, Collation collation);
    BsonValue GetResult();
    void Reset();
}