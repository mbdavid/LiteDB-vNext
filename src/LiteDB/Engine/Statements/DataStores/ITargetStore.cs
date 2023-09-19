namespace LiteDB.Engine;

public interface ITargetStore
{
    public byte ColID { get; }

    string Name { get; }

    ValueTask InsertAsync(BsonDocument documents, BsonAutoId autoId);
    ValueTask UpdateAsync(BsonDocument documents, BsonAutoId autoId);
    ValueTask DeleteAsync(BsonDocument documents, BsonAutoId autoId);

}
