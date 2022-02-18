namespace LiteDB;

internal class ArrayIndexBsonExpression : BsonExpression
{
    public override BsonExpressionType Type => BsonExpressionType.ArrayIndex;

    public BsonExpression Array { get; }

    public BsonExpression Index { get; }

    public ArrayIndexBsonExpression(BsonExpression array, BsonExpression index)
    {
        this.Array = array;
        this.Index = index;
    }

    internal override BsonValue Execute(BsonExpressionContext context)
    {
        var array = this.Array.Execute(context);
        var index = this.Index.Execute(context);

        if (!array.IsArray || !index.IsNumber) return BsonValue.Null;

        var arr = array.AsArray;
        var idx = index.AsInt32;

        // adding support for negative values (backward)
        var i = idx < 0 ? arr.Count + idx : idx;

        if (i >= arr.Count) return BsonValue.Null;

        return arr[i];
    }

    public override string ToString()
    {
        return this.Array.ToString() + "[" + this.Index.ToString() + "]";  
    }
}
