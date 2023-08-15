using Bogus.Bson;
using Castle.Core.Configuration;
using Newtonsoft.Json.Linq;
using static LiteDB.BsonExpression;

namespace LiteDB.Tests.Expressions;


public class Expressions_Tests
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

    private static BsonDocument doc = new BsonDocument
    {
        ["_id"] = 16,
        ["name"] = "Name Surname",
        ["age"] = 26,
        ["clients"] = new BsonArray()
        {
            new BsonDocument
            {
                ["name"] = "Jhon",
                ["age"] = 42
},
            new BsonDocument
            {
                ["name"] = "Fred",
                ["age"] = 16
},
            new BsonDocument
            {
                ["name"] = "Maria",
                ["age"] = 21
            }
        },
        ["arr"] = new BsonArray() { 1, 2, 3 },
        ["doc"] = new BsonDocument
        {
            ["arr"] = new BsonArray() { 10, 11, 12 }
        }
    };

    public static IEnumerable<object[]> Get_Expressions()
    {
        #region BasicTypes
        yield return new object[] { Constant(12), new BsonInt32(12) };
        yield return new object[] { Constant(2.6), new BsonDouble(2.6)};
        yield return new object[] { Constant(true), new BsonBoolean(true) };
        yield return new object[] { Constant("string"), new BsonString("string") };
        yield return new object[] { Array(12, 13, 14), new BsonArray { 12, 13, 14} };
        yield return new object[] { MakeDocument(new Dictionary<string, BsonExpression> { ["name"] = Constant("Maria") }), new BsonDocument{ ["name"] = "Maria" } };
        yield return new object[] { Root(), doc };
        yield return new object[] { Path(Root(), "age"), new BsonInt32(26) };
        #endregion

        #region InterTypesInteraction
        yield return new object[] { Add(Constant(12), Constant(14)), new BsonInt32(26) };
        yield return new object[] { Add(Constant(2.9), Constant(3)), new BsonDouble(5.9) };
        yield return new object[] { Add(Constant("Lite"), Constant("DB")), new BsonString("LiteDB") };
        yield return new object[] { Add(Constant(12), Constant("string")), new BsonString("12string") };
        yield return new object[] { Add(MakeDocument(new Dictionary<string, BsonExpression> { ["a"] = Constant(1) }), Constant("string")), new BsonString("{\"a\":1}string") };
        yield return new object[] { Add(Constant(1), "string"), BsonValue.Null };
        yield return new object[] { Add(Array(1, 2), Constant(3)), BsonValue.Null };
        #endregion

        #region DocumentRelated
        yield return new object[] { Path(Path(Root(), "doc"), "arr"), new BsonArray { 10, 11, 12 } };
        yield return new object[] { Path(Current(), "name"), new BsonString("Name Surname") };
        yield return new object[] { Filter(Path(Root(), "clients"), GreaterThanOrEqual(Path(Current(), "age"), Constant(18))), new BsonArray { new BsonDocument { ["name"] = "Jhon", ["age"] = 42 }, new BsonDocument { ["name"] = "Maria", ["age"] = 21 } } };
        yield return new object[] { Map(Path(Root(), "clients"), Path(Current(), "name")), new BsonArray { "Jhon", "Fred", "Maria" } };
        yield return new object[] { ArrayIndex(Path(Root(), "arr"), Constant(1)), new BsonInt32(2) };
        #endregion

        #region CallMethods
        #region Date
        //yield return new object[] { Call("YEAR", new BsonExpression[] { Constant(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc) }), new BsonInt32(2003) };
        #endregion

        #region Math
        yield return new object[] { Call("ABS", new BsonExpression[] { Constant(-10) }), new BsonInt32(10)};
        yield return new object[] { Call("ABS", new BsonExpression[] { Constant(-10.5) }), new BsonDouble(10.5) };
        yield return new object[] { Call("ROUND", new BsonExpression[] { Constant(2), Constant(1) }), new BsonInt32(2) };
        yield return new object[] { Call("ROUND", new BsonExpression[] { Constant(2.4), Constant(0) }), new BsonDouble(2) };
        yield return new object[] { Call("ROUND", new BsonExpression[] { Constant(2.5), Constant(0) }), new BsonDouble(2) };
        yield return new object[] { Call("ROUND", new BsonExpression[] { Constant(2.6), Constant(0) }), new BsonDouble(3) };
        yield return new object[] { Call("POW", new BsonExpression[] { Constant(2.0), Constant(3.0) }), new BsonDouble(8.0) };
        #endregion

        #region Misc
        yield return new object[] { Call("JSON", new BsonExpression[] { Constant("{a:1}") }), MakeDocument(new Dictionary<string, BsonExpression> { ["a"] = Constant(1) }) };
        //yield return new object[] { Call("EXTEND", new BsonExpression[] { MakeDocument(new Dictionary<string, BsonExpression> { ["b"] = Constant(2) }), MakeDocument(new Dictionary<string, BsonExpression> { ["a"] = Constant(1) }) }), new BsonDocument { ["b"] = 2, ["a"] = 1 } };
        yield return new object[] { Call("KEYS", new BsonExpression[] { MakeDocument(new Dictionary<string, BsonExpression> { ["name"] = Constant("Maria"), ["age"] = Constant(18) }) }), new BsonArray() { "name", "age" } };
        yield return new object[] { Call("VALUES", new BsonExpression[] { MakeDocument(new Dictionary<string, BsonExpression> { ["name"] = Constant("Maria"), ["age"] = Constant(18) }) }), new BsonArray() { "Maria", 18 } };
        //yield return new object[] { Call("OID_CREATIONTIME", new BsonExpression[] {  }),  };
        yield return new object[] { Call("COALESCE", new BsonExpression[] { Constant(10), Constant(20)}), new BsonInt32(10) };
        yield return new object[] { Call("COALESCE", new BsonExpression[] { Constant(BsonValue.Null), Constant(20) }), new BsonInt32(20) };
        yield return new object[] { Call("LENGTH", new BsonExpression[] { Constant("14LengthString") }), new BsonInt32(14) };
        yield return new object[] { Call("LENGTH", new BsonExpression[] { Constant(new BsonBinary(new byte[] { 255, 255, 255})) }), new BsonInt32(3) };
        yield return new object[] { Call("LENGTH", new BsonExpression[] { Array(10, 11, 12, 13) }), new BsonInt32(4) };
        yield return new object[] { Call("LENGTH", new BsonExpression[] { MakeDocument(new Dictionary<string, BsonExpression> { ["name"] = Constant("Maria"), ["age"] = Constant(18) }) }), new BsonInt32(2) };
        //TOP
        //UNION
        //EXCEPT
        //DISTINCT
        
        #endregion

        #region String
        yield return new object[] { Call("LOWER", new BsonExpression[] { Constant("LiteDB") }), new BsonString("litedb") };
        yield return new object[] { Call("UPPER", new BsonExpression[] { Constant("LiteDB") }), new BsonString("LITEDB") };
        yield return new object[] { Call("LTRIM", new BsonExpression[] { Constant("    LiteDB") }), new BsonString("LiteDB") };
        yield return new object[] { Call("RTRIM", new BsonExpression[] { Constant("LiteDB    ") }), new BsonString("LiteDB") };
        yield return new object[] { Call("TRIM", new BsonExpression[] { Constant("    LiteDB    ") }), new BsonString("LiteDB") };
        yield return new object[] { Call("INDEXOF", new BsonExpression[] { Constant("LiteDB"), Constant("D") }), new BsonInt32(4) };
        yield return new object[] { Call("INDEXOF", new BsonExpression[] { Constant("LiteDB-LiteDB"), Constant("D"), Constant(5) }), new BsonInt32(11) };
        yield return new object[] { Call("SUBSTRING", new BsonExpression[] { Constant("LiteDB-LiteDB"), Constant(4) }), new BsonString("DB-LiteDB") };
        yield return new object[] { Call("SUBSTRING", new BsonExpression[] { Constant("LiteDB-LiteDB"), Constant(4), Constant(2) }), new BsonString("DB") };
        yield return new object[] { Call("REPLACE", new BsonExpression[] { Constant("LiteDB"), Constant("t"), Constant("v") }), new BsonString("LiveDB") };
        yield return new object[] { Call("LPAD", new BsonExpression[] { Constant("LiteDB"), Constant(10), Constant("-") }), new BsonString("----LiteDB") };
        yield return new object[] { Call("RPAD", new BsonExpression[] { Constant("LiteDB"), Constant(10), Constant("-") }), new BsonString("LiteDB----") };
        yield return new object[] { Call("SPLIT", new BsonExpression[] { Constant("LiteDB-LiteDB"), Constant("-") }), new BsonArray() { "LiteDB", "LiteDB" } };
        yield return new object[] { Call("SPLIT", new BsonExpression[] { Constant("LiteDB-LiteDB"), Constant("(-)"), Constant(true) }), new BsonArray() { "LiteDB", "-", "LiteDB" } };
        yield return new object[] { Call("FORMAT", new BsonExpression[] { Constant(42), Constant("X") }), new BsonString("2A") };
        yield return new object[] { Call("JOIN", new BsonExpression[] { Array("LiteDB", "-LiteDB") }), new BsonString("LiteDB-LiteDB") };
        yield return new object[] { Call("JOIN", new BsonExpression[] { Array("LiteDB", "LiteDB"), Constant("/") }), new BsonString("LiteDB/LiteDB") };
        #endregion
        #endregion

        #region BinaryExpressions
        yield return new object[] { Add(Constant(1), Constant(2)), new BsonInt32(3) };
        yield return new object[] { Subtract(Constant(1), Constant(2)), new BsonInt32(-1) };
        yield return new object[] { Multiply(Constant(1), Constant(2)), new BsonInt32(2) };
        yield return new object[] { Divide(Constant(4), Constant(2)), new BsonInt32(2) };
        yield return new object[] { Modulo(Constant(1), Constant(2)), new BsonInt32(1) };
        yield return new object[] { Equal(Constant(1), Constant(2)), new BsonBoolean(false) };
        yield return new object[] { NotEqual(Constant(1), Constant(2)), new BsonBoolean(true) };
        yield return new object[] { GreaterThan(Constant(1), Constant(2)), new BsonBoolean(false) };
        yield return new object[] { GreaterThanOrEqual(Constant(1), Constant(2)), new BsonBoolean(false) };
        yield return new object[] { LessThan(Constant(1), Constant(2)), new BsonBoolean(true) };
        yield return new object[] { LessThanOrEqual(Constant(1), Constant(2)), new BsonBoolean(true) };
        yield return new object[] { Contains(Array(1, 2, 3), Constant(3)), new BsonBoolean(true) };
        yield return new object[] { Between(Constant(1), Array(2, 3)), new BsonBoolean(false) };
        yield return new object[] { Like(Constant("LiteDB"), Constant("L%")), new BsonBoolean(true) };
        yield return new object[] { In(Constant(1), Array(2, 3)), new BsonBoolean(false) };
        yield return new object[] { Or(Constant(true), Constant(false)), new BsonBoolean(true) };
        yield return new object[] { And(Constant(true), Constant(false)), new BsonBoolean(false) };
        #endregion
    }

    [Theory]
    [MemberData(nameof(Get_Expressions))]
    public void Execute_Theory(params object[] T)
    {
        T[0].As<BsonExpression>().Execute(doc).Should().Be(T[1] as BsonValue);
    }
}