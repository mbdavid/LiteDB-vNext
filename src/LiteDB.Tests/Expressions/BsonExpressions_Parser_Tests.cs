using Bogus.Bson;
using Castle.Core.Configuration;
using Newtonsoft.Json.Linq;
using static LiteDB.BsonExpression;

namespace LiteDB.Tests.Expressions;


public class BsonExpressions_Parser_Tests
{
    #region StaticAuxiliaryMethods
    private static BsonExpression Array(params BsonValue[] values)
    {
        var expressions = new List<BsonExpression>();
        foreach (BsonValue value in values)
        {
            expressions.Add(Constant(value));
        }
        return MakeArray(expressions);
    }
    #endregion



    public static IEnumerable<object[]> Get_Expressions()
    {
        #region BasicTypes
        yield return new object[] { "10", Constant(10) };
        yield return new object[] { "2.6", Constant(2.6) };
        yield return new object[] { "true", Constant(true) };
        yield return new object[] { "\"LiteDB\"", Constant("LiteDB") };
        yield return new object[] { "[12,13,14]", Array(12, 13, 14) };
        yield return new object[] { "{a:1}", MakeDocument(new Dictionary<string, BsonExpression> { ["a"] = Constant(1) }) };
        yield return new object[] { "@LiteDB", Parameter("LiteDB") };
        yield return new object[] { "$.field", Path(Root(), "field") };
        #endregion

        #region InterTypesInteraction
        yield return new object[] { "12+14", Add(Constant(12), Constant(14)) };
        yield return new object[] { "2.9+3", Add(Constant(2.9), Constant(3)) };
        yield return new object[] { "\"Lite\"+\"DB\"", Add(Constant("Lite"), Constant("DB")) };
        yield return new object[] { "12+\"string\"", Add(Constant(12), Constant("string")) };
        yield return new object[] { "{a:1}+\"string\"", Add(MakeDocument(new Dictionary<string, BsonExpression> { ["a"] = "1" }), Constant("string")) };
        yield return new object[] { "1+\"string\"", Add(Constant(1), Constant("string")) };
        yield return new object[] { "[1,2]+3", Add(Array(1, 2), Constant(3)) };
        #endregion

        #region DocumentRelated
        yield return new object[] { "$.clients", Path(Root(), "clients") };
        yield return new object[] { "$.doc.arr", Path(Path(Root(), "doc"), "arr") };
        yield return new object[] { "@.name", Path(Current(), "name") };
        yield return new object[] { "$.clients[age>=18]", Filter(Path(Root(), "clients"), GreaterThanOrEqual(Path(Current(), "age"), Constant(18))) };
        yield return new object[] { "$.clients=>@.name", Map(Path(Root(), "clients"), Path(Current(), "name")) };
        yield return new object[] { "$.arr[1]", ArrayIndex(Path(Root(), "arr"), Constant(1)) };
        #endregion

        #region BinaryExpressions
        yield return new object[] { "'LiteDB' LIKE 'L%'", Like(Constant("LiteDB"), Constant("L%")) };
        yield return new object[] { "1+2", Add(Constant(1), Constant(2)) };
        yield return new object[] { "1-2", Subtract(Constant(1), Constant(2)) };
        yield return new object[] { "1*2", Multiply(Constant(1), Constant(2)) };
        yield return new object[] { "1/2", Divide(Constant(1), Constant(2)) };
        yield return new object[] { "1%2", Modulo(Constant(1), Constant(2)) };
        yield return new object[] { "1=2", Equal(Constant(1), Constant(2)) };
        yield return new object[] { "1!=2", NotEqual(Constant(1), Constant(2)) };
        yield return new object[] { "1>2", GreaterThan(Constant(1), Constant(2)) };
        yield return new object[] { "1>=2", GreaterThanOrEqual(Constant(1), Constant(2)) };
        yield return new object[] { "1<2", LessThan(Constant(1), Constant(2)) };
        yield return new object[] { "1<=2", LessThanOrEqual(Constant(1), Constant(2)) };
        yield return new object[] { "[1,2] CONTAINS 3", Contains(Array(1, 2), Constant(3)) };
        yield return new object[] { "1 BETWEEN 2 AND 3", Between(Constant(1), Array(2, 3)) };
        yield return new object[] { "'LiteDB' LIKE 'L%'", Like(Constant("LiteDB"), Constant("L%")) };
        yield return new object[] { "1 IN [2,3]", In(Constant(1), Array(2, 3)) };
        yield return new object[] { "true OR false", Or(Constant(true), Constant(false)) };
        yield return new object[] { "true AND false", And(Constant(true), Constant(false)) };
        #endregion

        #region CallMethods
        yield return new object[] { "LOWER(\"LiteDB\")", Call("LOWER", new BsonExpression[] { Constant("LiteDB") }) };
        yield return new object[] { "UPPER(\"LiteDB\")", Call("UPPER", new BsonExpression[] { Constant("LiteDB") }) };
        yield return new object[] { "LTRIM(\"    LiteDB\")", Call("LTRIM", new BsonExpression[] { Constant("    LiteDB") }) };
        yield return new object[] { "RTRIM(\"LiteDB    \")", Call("RTRIM", new BsonExpression[] { Constant("LiteDB    ") }) };
        yield return new object[] { "TRIM(\"    LiteDB    \")", Call("TRIM", new BsonExpression[] { Constant("    LiteDB    ") }) };
        yield return new object[] { "INDEXOF(\"LiteDB\",\"D\")", Call("INDEXOF", new BsonExpression[] { Constant("LiteDB"), Constant("D") }) };
        yield return new object[] { "INDEXOF(\"LiteDB-LiteDB\",\"D\",5)", Call("INDEXOF", new BsonExpression[] { Constant("LiteDB-LiteDB"), Constant("D"), Constant(5) }) };
        yield return new object[] { "SUBSTRING(\"LiteDB-LiteDB\",4)", Call("SUBSTRING", new BsonExpression[] { Constant("LiteDB-LiteDB"), Constant(4) }) };
        yield return new object[] { "SUBSTRING(\"LiteDB-LiteDB\",4,2)", Call("SUBSTRING", new BsonExpression[] { Constant("LiteDB-LiteDB"), Constant(4), Constant(2) }) };
        yield return new object[] { "REPLACE(\"LiteDB\",\"t\",\"v\")", Call("REPLACE", new BsonExpression[] { Constant("LiteDB"), Constant("t"), Constant("v") }) };
        yield return new object[] { "LPAD(\"LiteDB\",10,\"-\")", Call("LPAD", new BsonExpression[] { Constant("LiteDB"), Constant(10), Constant("-") }) };
        yield return new object[] { "RPAD(\"LiteDB\",10,\"-\")", Call("RPAD", new BsonExpression[] { Constant("LiteDB"), Constant(10), Constant("-") }) };
        yield return new object[] { "SPLIT(\"LiteDB-LiteDB\",\"-\")", Call("SPLIT", new BsonExpression[] { Constant("LiteDB-LiteDB"), Constant("-") }) };
        yield return new object[] { "SPLIT(\"LiteDB-LiteDB\",\"(-)\",true)", Call("SPLIT", new BsonExpression[] { Constant("LiteDB-LiteDB"), Constant("(-)"), Constant(true) }) };
        yield return new object[] { "FORMAT(42,\"X\")", Call("FORMAT", new BsonExpression[] { Constant(42), Constant("X") }) };
        yield return new object[] { "JOIN([\"LiteDB\",\"-LiteDB\"])", Call("JOIN", new BsonExpression[] { Array("LiteDB", "-LiteDB") }) };
        yield return new object[] { "JOIN([\"LiteDB\",\"LiteDB\"],\"/\")", Call("JOIN", new BsonExpression[] { Array("LiteDB", "LiteDB"), Constant("/") }) };


        #endregion
    }

    [Theory]
    [MemberData(nameof(Get_Expressions))]
    public void Create_Theory(params object[] T)
    {
        var test = Create(T[0] as string);
        test.Should().Be(T[1] as BsonExpression);
    }

    [Theory]
    [InlineData("1 BETWEEN 1")]
    [InlineData("{a:1 b:1}")]
    [InlineData("[1,2 3]")]
    [InlineData("true OR (x>1")]
    [InlineData("UPPER('abc'")]
    [InlineData("INDEXOF('abc''b')")]
    public void Create_MisTyped_ShouldThrowException(string expr)
    {
        Assert.Throws<LiteException>(() => Create(expr));
    }
}