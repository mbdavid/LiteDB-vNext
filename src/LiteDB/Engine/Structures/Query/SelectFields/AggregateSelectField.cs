namespace LiteDB.Engine;

internal class AggregateSelectField : ISelectField
{
    public BsonExpression Expression => this.AggregateFunc.Expression;

    public IAggregateFunc AggregateFunc { get; }

    public AggregateSelectField(IAggregateFunc aggregateFunc)
    {
        this.AggregateFunc = aggregateFunc;
    }
}
