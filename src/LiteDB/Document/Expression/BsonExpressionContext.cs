namespace LiteDB;

internal class BsonExpressionContext
{
    public BsonValue Root { get; }
    public BsonValue Current { get; set; }
    public BsonDocument Parameters { get; }
    public Collation Collation { get; }

    public BsonExpressionContext(BsonValue root, BsonDocument parameters, Collation collation)
    {
        this.Root = root ?? new BsonDocument();
        this.Current = this.Root;
        this.Parameters = parameters ?? new BsonDocument();
        this.Collation = collation ?? Collation.Binary;
    }
}
