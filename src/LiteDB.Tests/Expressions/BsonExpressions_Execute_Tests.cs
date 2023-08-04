using Bogus.Bson;
using Castle.Core.Configuration;
using Newtonsoft.Json.Linq;

namespace LiteDB.Tests.Expressions;


public class Expressions_Tests
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
    private static BsonExpression Call(MethodInfo method, BsonExpression[] parameters)
    {
        return BsonExpression.Call(method, parameters);
    }

    private static MethodInfo GetMethod(string name, int paramCount)
    {
        return BsonExpression.GetMethod(name, paramCount);
    }

    private BsonDocument doc = new BsonDocument
    {
        ["_id"] = 16,
        ["name"] = "Name Surname",
        ["age"] = 26,
        ["arr"] = new BsonArray() { 1, 2, 3 },
        ["doc"] = new BsonDocument
        {
            ["arr"] = new BsonArray() { 10, 11, 12 }
        }
    };

    public static IEnumerable<object[]> Get_Expressions()
    {
        yield return new object[] { BsonExpression.Add(Constant(12), Constant(14)), new BsonInt32(26) };
        yield return new object[] { BsonExpression.Add(Constant(2.9), Constant(3)), new BsonDouble(5.9) };
        yield return new object[] { BsonExpression.Add(Constant("Lite"), Constant("DB")), new BsonString("LiteDB") };
        yield return new object[] { BsonExpression.Add(Constant(12), Constant("string")), new BsonString("12string") };
        yield return new object[] { BsonExpression.Add(BsonExpression.MakeDocument(new Dictionary<string, BsonExpression> { ["a"] = "1" }), Constant("string")), new BsonString("{\"a\":1}string") };
        yield return new object[] { BsonExpression.Add(Constant(1), "string"), BsonValue.Null };
        yield return new object[] { BsonExpression.Add(Array(1, 2), Constant(3)), BsonValue.Null };

        yield return new object[] { BsonExpression.Map(Array(1, 2), Constant(1)), new BsonArray() { 1, 1 } };
        yield return new object[] { BsonExpression.ArrayIndex(Array(1, 2, 3), Constant(2)), new BsonInt32(3) };
        yield return new object[] { Call(GetMethod("LOWER", 1), new BsonExpression[] { Constant("LiteDB") }), new BsonString("litedb") };
        yield return new object[] { Call(GetMethod("UPPER", 1), new BsonExpression[] { Constant("LiteDB") }), new BsonString("LITEDB") };
        yield return new object[] { Call(GetMethod("LTRIM", 1), new BsonExpression[] { Constant("    LiteDB") }), new BsonString("LiteDB") };
        yield return new object[] { Call(GetMethod("RTRIM", 1), new BsonExpression[] { Constant("LiteDB    ") }), new BsonString("LiteDB") };
        yield return new object[] { Call(GetMethod("TRIM", 1), new BsonExpression[] { Constant("    LiteDB    ") }), new BsonString("LiteDB") };
        yield return new object[] { Call(GetMethod("INDEXOF", 2), new BsonExpression[] { Constant("LiteDB"), Constant("D") }), new BsonInt32(4) };
        yield return new object[] { Call(GetMethod("INDEXOF", 3), new BsonExpression[] { Constant("LiteDB-LiteDB"), Constant("D"), Constant(5) }), new BsonInt32(11) };
        yield return new object[] { Call(GetMethod("SUBSTRING", 2), new BsonExpression[] { Constant("LiteDB-LiteDB"), Constant(4) }), new BsonString("DB-LiteDB") };
        yield return new object[] { Call(GetMethod("SUBSTRING", 3), new BsonExpression[] { Constant("LiteDB-LiteDB"), Constant(4), Constant(2) }), new BsonString("DB") };
        yield return new object[] { Call(GetMethod("REPLACE", 3), new BsonExpression[] { Constant("LiteDB"), Constant("t"), Constant("v") }), new BsonString("LiveDB") };
        yield return new object[] { Call(GetMethod("LPAD", 3), new BsonExpression[] { Constant("LiteDB"), Constant(10), Constant("-") }), new BsonString("----LiteDB") };
        yield return new object[] { Call(GetMethod("RPAD", 3), new BsonExpression[] { Constant("LiteDB"), Constant(10), Constant("-") }), new BsonString("LiteDB----") };
        yield return new object[] { Call(GetMethod("SPLIT", 2), new BsonExpression[] { Constant("LiteDB-LiteDB"), Constant("-") }), new BsonArray() { "LiteDB", "LiteDB" } };
        yield return new object[] { Call(GetMethod("SPLIT", 3), new BsonExpression[] { Constant("LiteDB-LiteDB"), Constant("(-)"), Constant(true) }), new BsonArray() { "LiteDB", "-", "LiteDB" } };
        yield return new object[] { Call(GetMethod("FORMAT", 2), new BsonExpression[] { Constant(42), Constant("X") }), new BsonString("2A") };
        yield return new object[] { Call(GetMethod("JOIN", 1), new BsonExpression[] { Array("LiteDB", "-LiteDB") }), new BsonString("LiteDB-LiteDB") };
        yield return new object[] { Call(GetMethod("JOIN", 2), new BsonExpression[] { Array("LiteDB", "LiteDB"), Constant("/") }), new BsonString("LiteDB/LiteDB") };

        yield return new object[] { new BinaryBsonExpression(BsonExpressionType.Add, Constant(1), Constant(2)), new BsonInt32(3) };
        yield return new object[] { new BinaryBsonExpression(BsonExpressionType.Subtract, Constant(1), Constant(2)), new BsonInt32(-1) };
        yield return new object[] { new BinaryBsonExpression(BsonExpressionType.Multiply, Constant(1), Constant(2)), new BsonInt32(2) };
        yield return new object[] { new BinaryBsonExpression(BsonExpressionType.Divide, Constant(4), Constant(2)), new BsonInt32(2) };
        yield return new object[] { new BinaryBsonExpression(BsonExpressionType.Modulo, Constant(1), Constant(2)), new BsonInt32(1) };
        yield return new object[] { new BinaryBsonExpression(BsonExpressionType.Equal, Constant(1), Constant(2)), new BsonBoolean(false) };
        yield return new object[] { new BinaryBsonExpression(BsonExpressionType.NotEqual, Constant(1), Constant(2)), new BsonBoolean(true) };
        yield return new object[] { new BinaryBsonExpression(BsonExpressionType.GreaterThan, Constant(1), Constant(2)), new BsonBoolean(false) };
        yield return new object[] { new BinaryBsonExpression(BsonExpressionType.GreaterThanOrEqual, Constant(1), Constant(2)), new BsonBoolean(false) };
        yield return new object[] { new BinaryBsonExpression(BsonExpressionType.LessThan, Constant(1), Constant(2)), new BsonBoolean(true) };
        yield return new object[] { new BinaryBsonExpression(BsonExpressionType.LessThanOrEqual, Constant(1), Constant(2)), new BsonBoolean(true) };
        yield return new object[] { new BinaryBsonExpression(BsonExpressionType.Contains, Array(1, 2, 3), Constant(3)), new BsonBoolean(true) };
        yield return new object[] { new BinaryBsonExpression(BsonExpressionType.Between, Constant(1), Array(2, 3)), new BsonBoolean(false) };
        yield return new object[] { new BinaryBsonExpression(BsonExpressionType.Like, Constant("LiteDB"), Constant("L%")), new BsonBoolean(true) };
        yield return new object[] { new BinaryBsonExpression(BsonExpressionType.In, Constant(1), Array(2, 3)), new BsonBoolean(false) };
        yield return new object[] { new BinaryBsonExpression(BsonExpressionType.Or, Constant(true), Constant(false)), new BsonBoolean(true) };
        yield return new object[] { new BinaryBsonExpression(BsonExpressionType.And, Constant(true), Constant(false)), new BsonBoolean(false) };
    }

    [Theory]
    [MemberData(nameof(Get_Expressions))]
    public void Execute_Theory(params object[] T)
    {
        T[0].As<BsonExpression>().Execute(doc).Should().Be(T[1] as BsonValue);
    }
}