namespace LiteDB.Engine;

/// <summary>
/// Calculate index cost based on expression/collection index. 
/// Lower cost is better - lowest will be selected
/// </summary>
internal struct IndexCost
{
    public int Cost { get; }
    public BsonExpression FilterExpression { get; }
    public BsonExpression IndexExpression { get; }
    public int Order { get; set; }


}