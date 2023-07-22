namespace LiteDB.Tests.Expressions;


public class Expressions_Tests
{
    /*[Theory]
    #region InlineData
    [InlineData("123", new BsonInt32(123))]
    [InlineData("2.9", 2.9)]
    //[InlineData("null", BsonValue.Null)]
    [InlineData("true", true)]
    [InlineData("false", false)]
    [InlineData("'string'", "string")]
    [InlineData("\"string\"", "string")]
    [InlineData("[]", "[]")]
    [InlineData("{a:1}", "{a:1}")]
    [InlineData("{a:true,i:0}", "{a:true,i:0}")]
    #endregion
    public void Execute_Constants_Theory(string exp, BsonValue res)
    {
        BsonExpression.Create(exp).Execute().Should().Be(res);
    }

    [Theory]
    #region InlineData
    [InlineData("3+4", 7)]
    [InlineData("2.9 + 1.2", 4.1)]
    [InlineData("'name'+' '+'surname'", "name surname")]
    #endregion
    public void Execute_Equations_Theory(string exp, BsonValue res)
    {
        BsonExpression.Create(exp).Execute().Should().Be(res);
    }*/

    [Theory]
    #region InlineData
    [InlineData("21", false)]
    [InlineData("word", false)]
    [InlineData("'string'", false)]
    [InlineData("1=1", true)]
    [InlineData("1=2", true)]
    [InlineData("2>1", true)]
    [InlineData("1>2", true)]
    [InlineData("1>=2", true)]
    [InlineData("2<1", true)]
    [InlineData("2<=1", true)]
    #endregion
    public void IsPredicate_Theory(string exp, bool isPredicate)
    {
        BsonExpression.Create(exp).IsPredicate.Should().Be(isPredicate);
    }

    [Fact]
    public void IsPredicate_Theory2()
    {
        BsonExpression.Create("true").IsPredicate.Should().Be(true);
    }


    [Theory]
    #region InlineData
    [InlineData("21", BsonExpressionType.Constant)]
    [InlineData("2.6", BsonExpressionType.Constant)]
    [InlineData("'string'", BsonExpressionType.Constant)]
    [InlineData("2+1", BsonExpressionType.Add)]
    [InlineData("2-1", BsonExpressionType.Subtract)]
    [InlineData("2*1", BsonExpressionType.Multiply)]
    [InlineData("2/1", BsonExpressionType.Divide)]
    [InlineData("[1,2,3]", BsonExpressionType.Array)]
    [InlineData("1=1", BsonExpressionType.Equal)]
    [InlineData("2!=1", BsonExpressionType.NotEqual)]
    [InlineData("2>1", BsonExpressionType.GreaterThan)]
    [InlineData("2>=1", BsonExpressionType.GreaterThanOrEqual)]
    [InlineData("1<2", BsonExpressionType.LessThan)]
    [InlineData("1<=2", BsonExpressionType.LessThanOrEqual)]
    [InlineData("@p0", BsonExpressionType.Parameter)]
    [InlineData("UPPER(@p0)", BsonExpressionType.Call)]
    [InlineData("'LiteDB' Like 'L%'", BsonExpressionType.Like)]
    [InlineData("7 BETWEEN 4 AND 10", BsonExpressionType.Between)]
    [InlineData("7 IN [1,4,7]", BsonExpressionType.In)]
    [InlineData("true AND true", BsonExpressionType.And)]
    [InlineData("true OR false", BsonExpressionType.Or)]
    [InlineData("arr=>@", BsonExpressionType.Map)]
    #endregion
    public void Type_Theory(string exp, BsonExpressionType type)
    {
        BsonExpression.Create(exp).Type.Should().Be(type);
    }
}