namespace LiteDB;

/// <summary>
/// </summary>
internal class AggregateAttribute: Attribute
{
    public Type AggregateType { get; }

    public AggregateAttribute(Type aggregateType)
    {
        this.AggregateType = aggregateType;
    }
}
