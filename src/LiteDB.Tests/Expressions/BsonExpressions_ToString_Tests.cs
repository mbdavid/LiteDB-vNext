using Bogus.Bson;
using Castle.Core.Configuration;
using Newtonsoft.Json.Linq;
using static LiteDB.BsonExpression;

namespace LiteDB.Tests.Expressions;



public class BsonExpressions_ToString_Tests
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

    public static IEnumerable<object[]> Get_ToStringExpressions()
    {
        #region BasicTypes
        yield return new object[] { Constant(10), "10" };
        yield return new object[] { Constant(2.6), "2.6" };
        yield return new object[] { Constant(true), "true" };
        yield return new object[] { Constant("LiteDB"), "\"LiteDB\"" };
        yield return new object[] { Array(12, 13, 14), "[12,13,14]" };
        yield return new object[] { MakeDocument(new Dictionary<string, BsonExpression> { ["a"] = Constant(1) }), "{a:1}" };
        yield return new object[] { Parameter("LiteDB"), "@LiteDB" };
        yield return new object[] { Root(), "$" };
        yield return new object[] { Path(Root(), "field"), "$.field" };
        #endregion

        #region InterTypesInteraction
        yield return new object[] { Add(Constant(12), Constant(14)), "12+14" };
        yield return new object[] { Add(Constant(2.9), Constant(3)), "2.9+3" };
        yield return new object[] { Add(Constant("Lite"), Constant("DB")), "\"Lite\"+\"DB\"" };
        yield return new object[] { Add(Constant(12), Constant("string")), "12+\"string\"" };
        yield return new object[] { Add(MakeDocument(new Dictionary<string, BsonExpression> { ["a"] = "1" }), Constant("string")), "{a:1}+\"string\"" };
        yield return new object[] { Add(Constant(1), Constant("string")), "1+\"string\"" };
        yield return new object[] { Add(Array(1, 2), Constant(3)), "[1,2]+3" };
        #endregion

        #region DocumentRelated
        yield return new object[] { new PathBsonExpression(Root(), "clients"), "$.clients" };
        yield return new object[] { new PathBsonExpression(new PathBsonExpression(Root(), "doc"), "arr"), "$.doc.arr" };
        yield return new object[] { new PathBsonExpression(Current(), "name"), "@.name" };
        yield return new object[] { new FilterBsonExpression(new PathBsonExpression(Root(), "clients"), new BinaryBsonExpression(BsonExpressionType.GreaterThanOrEqual, new PathBsonExpression(Current(), "age"), Constant(18))), "$.clients[@.age>=18]" };
        yield return new object[] { new MapBsonExpression(new PathBsonExpression(Root(), "clients"), new PathBsonExpression(Current(), "name")), "$.clients=>@.name" };
        yield return new object[] { new ArrayIndexBsonExpression(new PathBsonExpression(Root(), "arr"), Constant(1)), "$.arr[1]" };
        #endregion

        #region BinaryExpressions
        yield return new object[] { new BinaryBsonExpression(BsonExpressionType.Add, Constant(1), Constant(2)), "1+2" };
        yield return new object[] { new BinaryBsonExpression(BsonExpressionType.Subtract, Constant(1), Constant(2)), "1-2" };
        yield return new object[] { new BinaryBsonExpression(BsonExpressionType.Multiply, Constant(1), Constant(2)), "1*2" };
        yield return new object[] { new BinaryBsonExpression(BsonExpressionType.Divide, Constant(1), Constant(2)), "1/2" };
        yield return new object[] { new BinaryBsonExpression(BsonExpressionType.Modulo, Constant(1), Constant(2)), "1%2" };
        yield return new object[] { new BinaryBsonExpression(BsonExpressionType.Equal, Constant(1), Constant(2)), "1=2" };
        yield return new object[] { new BinaryBsonExpression(BsonExpressionType.NotEqual, Constant(1), Constant(2)), "1!=2" };
        yield return new object[] { new BinaryBsonExpression(BsonExpressionType.GreaterThan, Constant(1), Constant(2)), "1>2" };
        yield return new object[] { new BinaryBsonExpression(BsonExpressionType.GreaterThanOrEqual, Constant(1), Constant(2)), "1>=2" };
        yield return new object[] { new BinaryBsonExpression(BsonExpressionType.LessThan, Constant(1), Constant(2)), "1<2" };
        yield return new object[] { new BinaryBsonExpression(BsonExpressionType.LessThanOrEqual, Constant(1), Constant(2)), "1<=2" };
        yield return new object[] { new BinaryBsonExpression(BsonExpressionType.Contains, Array(1, 2), Constant(3)), "[1,2] CONTAINS 3" };
        yield return new object[] { new BinaryBsonExpression(BsonExpressionType.Between, Constant(1), Array(2, 3)), "1 BETWEEN 2 AND 3" };
        yield return new object[] { new BinaryBsonExpression(BsonExpressionType.Like, Constant("LiteDB"), Constant("L%")), "\"LiteDB\" LIKE \"L%\"" };
        yield return new object[] { new BinaryBsonExpression(BsonExpressionType.In, Constant(1), Array(2, 3)), "1 IN [2,3]" };
        yield return new object[] { new BinaryBsonExpression(BsonExpressionType.Or, Constant(true), Constant(false)), "true OR false" };
        yield return new object[] { new BinaryBsonExpression(BsonExpressionType.And, Constant(true), Constant(false)), "true AND false" };
        #endregion

        #region CallMethods
        yield return new object[] { Call(GetMethod("LOWER", 1), new BsonExpression[] { Constant("LiteDB") }), "LOWER(\"LiteDB\")" };
        yield return new object[] { Call(GetMethod("UPPER", 1), new BsonExpression[] { Constant("LiteDB") }), "UPPER(\"LiteDB\")" };
        yield return new object[] { Call(GetMethod("LTRIM", 1), new BsonExpression[] { Constant("    LiteDB") }), "LTRIM(\"    LiteDB\")" };
        yield return new object[] { Call(GetMethod("RTRIM", 1), new BsonExpression[] { Constant("LiteDB    ") }), "RTRIM(\"LiteDB    \")" };
        yield return new object[] { Call(GetMethod("TRIM", 1), new BsonExpression[] { Constant("    LiteDB    ") }), "TRIM(\"    LiteDB    \")" };
        yield return new object[] { Call(GetMethod("INDEXOF", 2), new BsonExpression[] { Constant("LiteDB"), Constant("D") }), "INDEXOF(\"LiteDB\",\"D\")" };
        yield return new object[] { Call(GetMethod("INDEXOF", 3), new BsonExpression[] { Constant("LiteDB-LiteDB"), Constant("D"), Constant(5) }), "INDEXOF(\"LiteDB-LiteDB\",\"D\",5)" };
        yield return new object[] { Call(GetMethod("SUBSTRING", 2), new BsonExpression[] { Constant("LiteDB-LiteDB"), Constant(4) }), "SUBSTRING(\"LiteDB-LiteDB\",4)" };
        yield return new object[] { Call(GetMethod("SUBSTRING", 3), new BsonExpression[] { Constant("LiteDB-LiteDB"), Constant(4), Constant(2) }), "SUBSTRING(\"LiteDB-LiteDB\",4,2)" };
        yield return new object[] { Call(GetMethod("REPLACE", 3), new BsonExpression[] { Constant("LiteDB"), Constant("t"), Constant("v") }), "REPLACE(\"LiteDB\",\"t\",\"v\")" };
        yield return new object[] { Call(GetMethod("LPAD", 3), new BsonExpression[] { Constant("LiteDB"), Constant(10), Constant("-") }), "LPAD(\"LiteDB\",10,\"-\")" };
        yield return new object[] { Call(GetMethod("RPAD", 3), new BsonExpression[] { Constant("LiteDB"), Constant(10), Constant("-") }), "RPAD(\"LiteDB\",10,\"-\")" };
        yield return new object[] { Call(GetMethod("SPLIT", 2), new BsonExpression[] { Constant("LiteDB-LiteDB"), Constant("-") }), "SPLIT(\"LiteDB-LiteDB\",\"-\")" };
        yield return new object[] { Call(GetMethod("SPLIT", 3), new BsonExpression[] { Constant("LiteDB-LiteDB"), Constant("(-)"), Constant(true) }), "SPLIT(\"LiteDB-LiteDB\",\"(-)\",true)" };
        yield return new object[] { Call(GetMethod("FORMAT", 2), new BsonExpression[] { Constant(42), Constant("X") }), "FORMAT(42,\"X\")" };
        yield return new object[] { Call(GetMethod("JOIN", 1), new BsonExpression[] { Array("LiteDB", "-LiteDB") }), "JOIN([\"LiteDB\",\"-LiteDB\"])" };
        yield return new object[] { Call(GetMethod("JOIN", 2), new BsonExpression[] { Array("LiteDB", "LiteDB"), Constant("/") }), "JOIN([\"LiteDB\",\"LiteDB\"],\"/\")" };
        #endregion
    }

    [Theory]
    [MemberData(nameof(Get_ToStringExpressions))]
    public void ToString_Theory(params object[] T)
    {
        var res = T[0].ToString();
        res.Should().Be(T[1] as string);
    }
}