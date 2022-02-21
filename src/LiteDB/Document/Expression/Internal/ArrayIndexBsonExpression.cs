namespace LiteDB;

internal class ArrayIndexBsonExpression : BsonExpression
{
    public override BsonExpressionType Type => BsonExpressionType.ArrayIndex;

    internal override IEnumerable<BsonExpression> Children => new[] { this.Array, this.Index };

    public BsonExpression Array { get; }

    public BsonExpression Index { get; }

    public ArrayIndexBsonExpression(BsonExpression array, BsonExpression index)
    {
        this.Array = array;
        this.Index = index;
    }

    internal override BsonValue Execute(BsonExpressionContext context)
    {
        var array = this.Array.Execute(context).AsArray;
        var index = this.Index.Execute(context);

        if (array == null || !index.IsNumber) return BsonValue.Null;

        var idx = index.AsInt32;

        // adding support for negative values (backward)
        var i = idx < 0 ? array.Count + idx : idx;

        if (i >= array.Count) return BsonValue.Null;

        return array[i];
    }

    public override string ToString()
    {
        return this.Array.ToString() + "[" + this.Index.ToString() + "]";  
    }
}
