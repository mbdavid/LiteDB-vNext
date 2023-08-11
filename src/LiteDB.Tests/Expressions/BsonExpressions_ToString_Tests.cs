using Bogus.Bson;
using Castle.Core.Configuration;
using Newtonsoft.Json.Linq;
using System;
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

    public static IEnumerable<object[]> Get_Expressions()
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
        yield return new object[] { Path(Root(), "clients"), "$.clients" };
        yield return new object[] { Path(Path(Root(), "doc"), "arr"), "$.doc.arr" };
        yield return new object[] { Path(Current(), "name"), "@.name" };
        yield return new object[] { Filter(Path(Root(), "clients"), GreaterThanOrEqual(Path(Current(), "age"), Constant(18))), "$.clients[@.age>=18]" };
        yield return new object[] { Map(Path(Root(), "clients"), Path(Current(), "name")), "$.clients=>@.name" };
        yield return new object[] { ArrayIndex(Path(Root(), "arr"), Constant(1)), "$.arr[1]" };
        #endregion

        #region BinaryExpressions
        yield return new object[] { Add(Constant(1), Constant(2)), "1+2" };
        yield return new object[] { Subtract(Constant(1), Constant(2)), "1-2" };
        yield return new object[] { Multiply(Constant(1), Constant(2)), "1*2" };
        yield return new object[] { Divide(Constant(1), Constant(2)), "1/2" };
        yield return new object[] { Modulo(Constant(1), Constant(2)), "1%2" };
        yield return new object[] { Equal(Constant(1), Constant(2)), "1=2" };
        yield return new object[] { NotEqual(Constant(1), Constant(2)), "1!=2" };
        yield return new object[] { GreaterThan(Constant(1), Constant(2)), "1>2" };
        yield return new object[] { GreaterThanOrEqual(Constant(1), Constant(2)), "1>=2" };
        yield return new object[] { LessThan(Constant(1), Constant(2)), "1<2" };
        yield return new object[] { LessThanOrEqual(Constant(1), Constant(2)), "1<=2" };
        yield return new object[] { Contains(Array(1, 2), Constant(3)), "[1,2] CONTAINS 3" };
        yield return new object[] { Between(Constant(1), Array(2, 3)), "1 BETWEEN 2 AND 3" };
        yield return new object[] { Like(Constant("LiteDB"), Constant("L%")), "\"LiteDB\" LIKE \"L%\"" };
        yield return new object[] { In(Constant(1), Array(2, 3)), "1 IN [2,3]" };
        yield return new object[] { Or(Constant(true), Constant(false)), "true OR false" };
        yield return new object[] { And(Constant(true), Constant(false)), "true AND false" };
        #endregion

        #region CallMethods
        #region Date
        //yield return new object[] { Call("YEAR", new BsonExpression[] { Constant(new DateTime(2003, 1, 10)) }), "YEAR(\"10/01/2003\")" };
        #endregion

        #region Math
        yield return new object[] { Call("ABS", new BsonExpression[] { Constant(-10) }), "ABS(-10)" };
        yield return new object[] { Call("ABS", new BsonExpression[] { Constant(-10.5) }), "ABS(-10.5)" };
        yield return new object[] { Call("ROUND", new BsonExpression[] { Constant(2), Constant(1) }), "ROUND(2,1)" };
        yield return new object[] { Call("ROUND", new BsonExpression[] { Constant(2.4), Constant(0) }), "ROUND(2.4,0)" };
        yield return new object[] { Call("ROUND", new BsonExpression[] { Constant(2.5), Constant(0) }), "ROUND(2.5,0)" };
        yield return new object[] { Call("ROUND", new BsonExpression[] { Constant(2.6), Constant(0) }), "ROUND(2.6,0)" };
        yield return new object[] { Call("POW", new BsonExpression[] { Constant(2), Constant(3) }), "POW(2,3)" };
        #endregion

        #region Misc
        yield return new object[] { Call("JSON", new BsonExpression[] { Constant("{a:1}") }), "JSON(\"{a:1}\")" };
        yield return new object[] { Call("EXTEND", new BsonExpression[] { Root(), MakeDocument(new Dictionary<string, BsonExpression> { ["a"] = Constant(1) }) }), "EXTEND($,{a:1})" };
        #endregion

        #region String
        yield return new object[] { Call("LOWER", new BsonExpression[] { Constant("LiteDB") }), "LOWER(\"LiteDB\")" };
        yield return new object[] { Call("UPPER", new BsonExpression[] { Constant("LiteDB") }), "UPPER(\"LiteDB\")" };
        yield return new object[] { Call("LTRIM", new BsonExpression[] { Constant("    LiteDB") }), "LTRIM(\"    LiteDB\")" };
        yield return new object[] { Call("RTRIM", new BsonExpression[] { Constant("LiteDB    ") }), "RTRIM(\"LiteDB    \")" };
        yield return new object[] { Call("TRIM", new BsonExpression[] { Constant("    LiteDB    ") }), "TRIM(\"    LiteDB    \")" };
        yield return new object[] { Call("INDEXOF", new BsonExpression[] { Constant("LiteDB"), Constant("D") }), "INDEXOF(\"LiteDB\",\"D\")" };
        yield return new object[] { Call("INDEXOF", new BsonExpression[] { Constant("LiteDB-LiteDB"), Constant("D"), Constant(5) }), "INDEXOF(\"LiteDB-LiteDB\",\"D\",5)" };
        yield return new object[] { Call("SUBSTRING", new BsonExpression[] { Constant("LiteDB-LiteDB"), Constant(4) }), "SUBSTRING(\"LiteDB-LiteDB\",4)" };
        yield return new object[] { Call("SUBSTRING", new BsonExpression[] { Constant("LiteDB-LiteDB"), Constant(4), Constant(2) }), "SUBSTRING(\"LiteDB-LiteDB\",4,2)" };
        yield return new object[] { Call("REPLACE", new BsonExpression[] { Constant("LiteDB"), Constant("t"), Constant("v") }), "REPLACE(\"LiteDB\",\"t\",\"v\")" };
        yield return new object[] { Call("LPAD", new BsonExpression[] { Constant("LiteDB"), Constant(10), Constant("-") }), "LPAD(\"LiteDB\",10,\"-\")" };
        yield return new object[] { Call("RPAD", new BsonExpression[] { Constant("LiteDB"), Constant(10), Constant("-") }), "RPAD(\"LiteDB\",10,\"-\")" };
        yield return new object[] { Call("SPLIT", new BsonExpression[] { Constant("LiteDB-LiteDB"), Constant("-") }), "SPLIT(\"LiteDB-LiteDB\",\"-\")" };
        yield return new object[] { Call("SPLIT", new BsonExpression[] { Constant("LiteDB-LiteDB"), Constant("(-)"), Constant(true) }), "SPLIT(\"LiteDB-LiteDB\",\"(-)\",true)" };
        yield return new object[] { Call("FORMAT", new BsonExpression[] { Constant(42), Constant("X") }), "FORMAT(42,\"X\")" };
        yield return new object[] { Call("JOIN", new BsonExpression[] { Array("LiteDB", "-LiteDB") }), "JOIN([\"LiteDB\",\"-LiteDB\"])" };
        yield return new object[] { Call("JOIN", new BsonExpression[] { Array("LiteDB", "LiteDB"), Constant("/") }), "JOIN([\"LiteDB\",\"LiteDB\"],\"/\")" };
        #endregion
        #endregion
    }

    [Theory]
    [MemberData(nameof(Get_Expressions))]
    public void ToString_Theory(params object[] T)
    {
        var res = T[0].ToString();
        res.Should().Be(T[1] as string);
    }
}