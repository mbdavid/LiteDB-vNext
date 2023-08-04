using Bogus.Bson;
using Castle.Core.Configuration;
using Newtonsoft.Json.Linq;

namespace LiteDB.Tests.Expressions;


public class BsonExpressions_Parser_Tests
{
    private static BsonExpression Constant(BsonValue value)
    {
        return BsonExpression.Constant(value);
    }

    private static BsonExpression Array(params BsonValue[] values)
    {
        var expressions = new List<BsonExpression>();
        foreach (BsonValue value in values)
        {
            expressions.Add(Constant(value));
        }
        return BsonExpression.MakeArray(expressions);
    }



    public static IEnumerable<object[]> Get_Methods()
    {
        yield return new object[] { "$.arr[x=1]", new FilterBsonExpression("$.arr", "@.x=1")};
        yield return new object[] { "$.arr", new PathBsonExpression(BsonExpression.Root(), "arr")};
        yield return new object[] { "$.doc.arr", new PathBsonExpression("$.doc", "arr")};
        yield return new object[] { "$.arr=>x", new MapBsonExpression("$.arr", "@.x")};
        yield return new object[] { "$.arr[1]", new ArrayIndexBsonExpression("$.arr", "1")};

        yield return new object[] { "'LiteDB' LIKE 'L%'", new BinaryBsonExpression(BsonExpressionType.Like, Constant("LiteDB"), Constant("L%"))};
        yield return new object[] { "1+2", new BinaryBsonExpression(BsonExpressionType.Add, Constant(1), Constant(2))};
        yield return new object[] { "1-2", new BinaryBsonExpression(BsonExpressionType.Subtract, Constant(1), Constant(2))};
        yield return new object[] { "1*2", new BinaryBsonExpression(BsonExpressionType.Multiply, Constant(1), Constant(2))};
        yield return new object[] { "1/2", new BinaryBsonExpression(BsonExpressionType.Divide, Constant(1), Constant(2))};
        yield return new object[] { "1%2", new BinaryBsonExpression(BsonExpressionType.Modulo, Constant(1), Constant(2))};
        yield return new object[] { "1=2", new BinaryBsonExpression(BsonExpressionType.Equal, Constant(1), Constant(2))};
        yield return new object[] { "1!=2", new BinaryBsonExpression(BsonExpressionType.NotEqual, Constant(1), Constant(2))};
        yield return new object[] { "1>2", new BinaryBsonExpression(BsonExpressionType.GreaterThan, Constant(1), Constant(2))};
        yield return new object[] { "1>=2", new BinaryBsonExpression(BsonExpressionType.GreaterThanOrEqual, Constant(1), Constant(2))};
        yield return new object[] { "1<2", new BinaryBsonExpression(BsonExpressionType.LessThan, Constant(1), Constant(2))};
        yield return new object[] { "1<=2", new BinaryBsonExpression(BsonExpressionType.LessThanOrEqual, Constant(1), Constant(2))};
        yield return new object[] { "[1,2] CONTAINS 3", new BinaryBsonExpression(BsonExpressionType.Contains, Array(1, 2), Constant(3))};
        yield return new object[] { "1 BETWEEN 2 AND 3", new BinaryBsonExpression(BsonExpressionType.Between, Constant(1), Array(2, 3))};
        yield return new object[] { "'LiteDB' LIKE 'L%'", new BinaryBsonExpression(BsonExpressionType.Like, Constant("LiteDB"), Constant("L%"))};
        yield return new object[] { "1 IN [2,3]", new BinaryBsonExpression(BsonExpressionType.In, Constant(1), Array(2, 3))};
        yield return new object[] { "true OR false", new BinaryBsonExpression(BsonExpressionType.Or, Constant(true), Constant(false))};
        yield return new object[] { "true AND false", new BinaryBsonExpression(BsonExpressionType.And, Constant(true), Constant(false))};
    }

    [Theory]
    [MemberData(nameof(Get_Methods))]
    public void Create_Theory(params object[] T)
    {
        BsonExpression.Create(T[0] as string).Should().Be(T[1] as BsonExpression);
    }

    [Theory]
    [InlineData("1 BETWEEN 1")]
    [InlineData("{a:1 b:1}")]
    [InlineData("[1,2 3]")]
    [InlineData("true OR (x>1")]
    [InlineData("UPPER('abc'")]
    [InlineData("INDEXOF('abc''b')")]
    public void Execute_Constants(string expr)
    {
        Assert.Throws<LiteException>(() => BsonExpression.Create(expr));
    }
}