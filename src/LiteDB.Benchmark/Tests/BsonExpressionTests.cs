namespace LiteDB.Benchmark;


[RPlotExporter]
[MemoryDiagnoser]
public class BsonExpressionTests
{
    [Benchmark]
    public void ExprExec() => BsonExpression.Create("(45 + 12 * a) > 99").Execute(new BsonDocument { ["a"] = 34m });

    //[Benchmark]
    //public void v5ExprExec() => v5.BsonExpression.Create("45 + 12 * a").Execute(new v5.BsonDocument { ["a"] = 34m });
}
