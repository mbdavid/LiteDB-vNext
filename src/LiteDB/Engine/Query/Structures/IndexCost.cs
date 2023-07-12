namespace LiteDB.Engine;

/// <summary>
/// Calculate index cost based on expression/collection index. 
/// Lower cost is better - lowest will be selected
/// </summary>
internal struct IndexCost
{
    public uint Cost { get; }

    ///// <summary>
    ///// Get filter expression: "$._id = 10"
    ///// </summary>
    //public BsonExpression Expression { get; }

    /// <summary>
    /// Get selected index document used in this selection
    /// </summary>
    public IndexDocument IndexDocument { get; }

    public int Order { get; }

    public IndexCost(BsonExpression expression)
    {

    }


    public IPipeEnumerator CreateIndex()
    {
        throw new NotImplementedException();
    }

}