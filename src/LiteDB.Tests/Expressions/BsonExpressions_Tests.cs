using Bogus.Bson;
using Castle.Core.Configuration;
using Newtonsoft.Json.Linq;

namespace LiteDB.Tests.Expressions;


public class Expressions_Tests
{

    private static IEnumerable<(string expression, BsonExpression result)> Get_Methods()
    {
        yield return ("$.arr[x=1]", new FilterBsonExpression("$.arr", "@.x=1"));
        yield return ("$.arr", new PathBsonExpression(BsonExpression.Root(), "arr"));
        yield return ("$.doc.arr", new PathBsonExpression("$.doc", "arr"));
        yield return ("$.arr=>x", new MapBsonExpression("$.arr", "@.x"));
        yield return ("$.arr[1]", new ArrayIndexBsonExpression("$.arr", "1"));

        yield return ("'LiteDB' LIKE 'L%'", new BinaryBsonExpression(BsonExpressionType.Like, BsonExpression.Constant("LiteDB"), BsonExpression.Constant("L%")));
        yield return ("1+2", new BinaryBsonExpression(BsonExpressionType.Add, BsonExpression.Constant(1), BsonExpression.Constant(2)));
        yield return ("1-2", new BinaryBsonExpression(BsonExpressionType.Subtract, BsonExpression.Constant(1), BsonExpression.Constant(2)));
        yield return ("1*2", new BinaryBsonExpression(BsonExpressionType.Multiply, BsonExpression.Constant(1), BsonExpression.Constant(2)));
        yield return ("1/2", new BinaryBsonExpression(BsonExpressionType.Divide, BsonExpression.Constant(1), BsonExpression.Constant(2)));
        yield return ("1%2", new BinaryBsonExpression(BsonExpressionType.Modulo, BsonExpression.Constant(1), BsonExpression.Constant(2)));
        yield return ("1=2", new BinaryBsonExpression(BsonExpressionType.Equal, BsonExpression.Constant(1), BsonExpression.Constant(2)));
        yield return ("1!=2", new BinaryBsonExpression(BsonExpressionType.NotEqual, BsonExpression.Constant(1), BsonExpression.Constant(2)));
        yield return ("1>2", new BinaryBsonExpression(BsonExpressionType.GreaterThan, BsonExpression.Constant(1), BsonExpression.Constant(2)));
        yield return ("1>=2", new BinaryBsonExpression(BsonExpressionType.GreaterThanOrEqual, BsonExpression.Constant(1), BsonExpression.Constant(2)));
        yield return ("1<2", new BinaryBsonExpression(BsonExpressionType.LessThan, BsonExpression.Constant(1), BsonExpression.Constant(2)));
        yield return ("1<=2", new BinaryBsonExpression(BsonExpressionType.LessThanOrEqual, BsonExpression.Constant(1), BsonExpression.Constant(2)));
        yield return ("[1,2] CONTAINS 3", new BinaryBsonExpression(BsonExpressionType.Contains, BsonExpression.MakeArray(new BsonExpression[] { BsonExpression.Constant(1), BsonExpression.Constant(2) }), BsonExpression.Constant(3)));
        yield return ("1 BETWEEN 2 AND 3", new BinaryBsonExpression(BsonExpressionType.Between, BsonExpression.Constant(1), BsonExpression.MakeArray(new BsonExpression[] { BsonExpression.Constant(2), BsonExpression.Constant(3) })));
        yield return ("'LiteDB' LIKE 'L%'", new BinaryBsonExpression(BsonExpressionType.Like, BsonExpression.Constant("LiteDB"), BsonExpression.Constant("L%")));
        yield return ("1 IN [2,3]", new BinaryBsonExpression(BsonExpressionType.In, BsonExpression.Constant(1), BsonExpression.MakeArray(new BsonExpression[] { BsonExpression.Constant(2), BsonExpression.Constant(3) })));
        yield return ("true OR false", new BinaryBsonExpression(BsonExpressionType.Or, BsonExpression.Constant(true), BsonExpression.Constant(false)));
        yield return ("true AND false", new BinaryBsonExpression(BsonExpressionType.And, BsonExpression.Constant(true), BsonExpression.Constant(false)));
    }

    [Fact]
    public void Create_Theory()
    {
        foreach (var T in Get_Methods())
        {
            BsonExpression.Create(T.expression).Should().Be(T.result);
        }
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

    public static IEnumerable<(BsonExpression expression, BsonValue result)> Get_Expressions()
    {
        yield return (BsonExpression.Add(BsonExpression.Constant(12), BsonExpression.Constant(14)), 26);
        yield return (BsonExpression.Add(BsonExpression.Constant(2.9), BsonExpression.Constant(3)), 5.9);
        yield return (BsonExpression.Add(BsonExpression.Constant("Lite"), BsonExpression.Constant("DB")), "LiteDB");
        yield return (BsonExpression.Add(BsonExpression.Constant(12), BsonExpression.Constant("string")), "12string");
        yield return (BsonExpression.Add(BsonExpression.MakeDocument(new Dictionary<string, BsonExpression> { ["a"] = "1" }), BsonExpression.Constant("string")), "{a:1}string");
        yield return (BsonExpression.Add(BsonExpression.Constant(1), "string"), BsonValue.Null);
        yield return (BsonExpression.Add(BsonExpression.MakeArray(new BsonExpression[] { BsonExpression.Constant(1), BsonExpression.Constant(2) }), BsonExpression.Constant(3)), BsonValue.Null);

        yield return (BsonExpression.Map(BsonExpression.MakeArray(new BsonExpression[] { BsonExpression.Constant(1), BsonExpression.Constant(2) }), BsonExpression.Constant(1)), new BsonArray() { 1, 1 });
        yield return (BsonExpression.ArrayIndex(BsonExpression.MakeArray(new BsonExpression[] { BsonExpression.Constant(1), BsonExpression.Constant(2), BsonExpression.Constant(3) }), BsonExpression.Constant(2)), 3);
        yield return (BsonExpression.Call(BsonExpression.GetMethod("LOWER", 1), new BsonExpression[] { BsonExpression.Constant("LiteDB") }), "litedb");
        yield return (BsonExpression.Call(BsonExpression.GetMethod("UPPER", 1), new BsonExpression[] { BsonExpression.Constant("LiteDB") }), "LITEDB");
        yield return (BsonExpression.Call(BsonExpression.GetMethod("LTRIM", 1), new BsonExpression[] { BsonExpression.Constant("    LiteDB") }), "LiteDB");
        yield return (BsonExpression.Call(BsonExpression.GetMethod("RTRIM", 1), new BsonExpression[] { BsonExpression.Constant("LiteDB    ") }), "LiteDB");
        yield return (BsonExpression.Call(BsonExpression.GetMethod("TRIM", 1), new BsonExpression[] { BsonExpression.Constant("    LiteDB    ") }), "LiteDB");
        yield return (BsonExpression.Call(BsonExpression.GetMethod("INDEXOF", 2), new BsonExpression[] { BsonExpression.Constant("LiteDB"), BsonExpression.Constant("D") }), 4);
        yield return (BsonExpression.Call(BsonExpression.GetMethod("INDEXOF", 3), new BsonExpression[] { BsonExpression.Constant("LiteDB-LiteDB"), BsonExpression.Constant("D"), BsonExpression.Constant(5) }), 11);
        yield return (BsonExpression.Call(BsonExpression.GetMethod("SUBSTRING", 2), new BsonExpression[] { BsonExpression.Constant("LiteDB-LiteDB"), BsonExpression.Constant(4) }), "DB-LiteDB");
        yield return (BsonExpression.Call(BsonExpression.GetMethod("SUBSTRING", 3), new BsonExpression[] { BsonExpression.Constant("LiteDB-LiteDB"), BsonExpression.Constant(4), BsonExpression.Constant(2) }), "DB");
        yield return (BsonExpression.Call(BsonExpression.GetMethod("REPLACE", 3), new BsonExpression[] { BsonExpression.Constant("LiteDB"), BsonExpression.Constant("t"), BsonExpression.Constant("v") }), "LiveDB");
        yield return (BsonExpression.Call(BsonExpression.GetMethod("LPAD", 3), new BsonExpression[] { BsonExpression.Constant("LiteDB"), BsonExpression.Constant(10), BsonExpression.Constant("-") }), "----LiteDB");
        yield return (BsonExpression.Call(BsonExpression.GetMethod("RPAD", 3), new BsonExpression[] { BsonExpression.Constant("LiteDB"), BsonExpression.Constant(10), BsonExpression.Constant("-") }), "LiteDB----");
        yield return (BsonExpression.Call(BsonExpression.GetMethod("SPLIT", 2), new BsonExpression[] { BsonExpression.Constant("LiteDB-LiteDB"), BsonExpression.Constant("-") }), new BsonArray() { "LiteDB", "LiteDB" });
        yield return (BsonExpression.Call(BsonExpression.GetMethod("SPLIT", 3), new BsonExpression[] { BsonExpression.Constant("LiteDB-LiteDB"), BsonExpression.Constant("(-)"), BsonExpression.Constant(true) }), new BsonArray() { "LiteDB", "-", "LiteDB" });
        yield return (BsonExpression.Call(BsonExpression.GetMethod("FORMAT", 2), new BsonExpression[] { BsonExpression.Constant(42), BsonExpression.Constant("X") }), "2A");
        yield return (BsonExpression.Call(BsonExpression.GetMethod("JOIN", 1), new BsonExpression[] { BsonExpression.MakeArray(new BsonExpression[] { BsonExpression.Constant("LiteDB"), BsonExpression.Constant("-LiteDB") }) }), "LiteDB-LiteDB");
        yield return (BsonExpression.Call(BsonExpression.GetMethod("JOIN", 2), new BsonExpression[] { BsonExpression.MakeArray(new BsonExpression[] { BsonExpression.Constant("LiteDB"), BsonExpression.Constant("LiteDB") }), BsonExpression.Constant("/") }), "LiteDB/LiteDB");

        yield return (new BinaryBsonExpression(BsonExpressionType.Add, BsonExpression.Constant(1), BsonExpression.Constant(2)), 3);
        yield return (new BinaryBsonExpression(BsonExpressionType.Subtract, BsonExpression.Constant(1), BsonExpression.Constant(2)), -1);
        yield return (new BinaryBsonExpression(BsonExpressionType.Multiply, BsonExpression.Constant(1), BsonExpression.Constant(2)), 2);
        yield return (new BinaryBsonExpression(BsonExpressionType.Divide, BsonExpression.Constant(4), BsonExpression.Constant(2)), 2);
        yield return (new BinaryBsonExpression(BsonExpressionType.Modulo, BsonExpression.Constant(1), BsonExpression.Constant(2)), 1);
        yield return (new BinaryBsonExpression(BsonExpressionType.Equal, BsonExpression.Constant(1), BsonExpression.Constant(2)), false);
        yield return (new BinaryBsonExpression(BsonExpressionType.NotEqual, BsonExpression.Constant(1), BsonExpression.Constant(2)), true);
        yield return (new BinaryBsonExpression(BsonExpressionType.GreaterThan, BsonExpression.Constant(1), BsonExpression.Constant(2)), false);
        yield return (new BinaryBsonExpression(BsonExpressionType.GreaterThanOrEqual, BsonExpression.Constant(1), BsonExpression.Constant(2)), false);
        yield return (new BinaryBsonExpression(BsonExpressionType.LessThan, BsonExpression.Constant(1), BsonExpression.Constant(2)), true);
        yield return (new BinaryBsonExpression(BsonExpressionType.LessThanOrEqual, BsonExpression.Constant(1), BsonExpression.Constant(2)), true);
        yield return (new BinaryBsonExpression(BsonExpressionType.Contains, BsonExpression.MakeArray(new BsonExpression[] { BsonExpression.Constant(1), BsonExpression.Constant(2), BsonExpression.Constant(3) }), BsonExpression.Constant(3)), true);
        yield return (new BinaryBsonExpression(BsonExpressionType.Between, BsonExpression.Constant(1), BsonExpression.MakeArray(new BsonExpression[] { BsonExpression.Constant(2), BsonExpression.Constant(3) })), false);
        yield return (new BinaryBsonExpression(BsonExpressionType.Like, BsonExpression.Constant("LiteDB"), BsonExpression.Constant("L%")), true);
        yield return (new BinaryBsonExpression(BsonExpressionType.In, BsonExpression.Constant(1), BsonExpression.MakeArray(new BsonExpression[] { BsonExpression.Constant(2), BsonExpression.Constant(3) })), false);
        yield return (new BinaryBsonExpression(BsonExpressionType.Or, BsonExpression.Constant(true), BsonExpression.Constant(false)), true);
        yield return (new BinaryBsonExpression(BsonExpressionType.And, BsonExpression.Constant(true), BsonExpression.Constant(false)), false);
    }

    [Fact]
    public void Execute_Theory()
    {
        foreach (var T in Get_Expressions())
        {
            T.expression.Execute(doc).Should().Be(T.result);
        }
    }

    public static IEnumerable<(BsonExpression expression, BsonValue result)> Get_ToStringExpressions()
    {
        yield return (BsonExpression.Add(BsonExpression.Constant(12), BsonExpression.Constant(14)), "12+14");
        yield return (BsonExpression.Add(BsonExpression.Constant(2.9), BsonExpression.Constant(3)), "2.9+3");
        yield return (BsonExpression.Add(BsonExpression.Constant("Lite"), BsonExpression.Constant("DB")), "Lite+DB");
        yield return (BsonExpression.Add(BsonExpression.Constant(12), BsonExpression.Constant("string")), "12+string");
        yield return (BsonExpression.Add(BsonExpression.MakeDocument(new Dictionary<string, BsonExpression> { ["a"] = "1" }), BsonExpression.Constant("string")), "{a:1}+string");
        yield return (BsonExpression.Add(BsonExpression.Constant(1), BsonExpression.Constant("string")), "1+string");
        yield return (BsonExpression.Add(BsonExpression.MakeArray(new BsonExpression[] { BsonExpression.Constant(1), BsonExpression.Constant(2) }), BsonExpression.Constant(3)), "[1,2]+3");

        yield return (BsonExpression.Constant(10), "10");
        yield return (BsonExpression.Constant(2.6), "2.6");
        yield return (BsonExpression.Constant("{a:1}"), "{a:1}");
        yield return (BsonExpression.Constant("LiteDB"), "LiteDB");
        yield return (BsonExpression.Parameter("LiteDB"), "@LiteDB");
        yield return (BsonExpression.Root(), "$");
        yield return (BsonExpression.Path(BsonExpression.Root(), "field"), "$.field");

        yield return (BsonExpression.Map(BsonExpression.MakeArray(new BsonExpression[] { BsonExpression.Constant(1), BsonExpression.Constant(2) }), BsonExpression.Constant(1)), "[1,2]=>1");
        yield return (BsonExpression.ArrayIndex(BsonExpression.MakeArray(new BsonExpression[] { BsonExpression.Constant(1), BsonExpression.Constant(2), BsonExpression.Constant(3) }), BsonExpression.Constant(2)), "[1,2,3][2]");
        yield return (BsonExpression.Call(BsonExpression.GetMethod("LOWER", 1), new BsonExpression[] { BsonExpression.Constant("LiteDB") }), "LOWER(LiteDB)");
        yield return (BsonExpression.Call(BsonExpression.GetMethod("UPPER", 1), new BsonExpression[] { BsonExpression.Constant("LiteDB") }), "UPPER(LiteDB)");
        yield return (BsonExpression.Call(BsonExpression.GetMethod("LTRIM", 1), new BsonExpression[] { BsonExpression.Constant("    LiteDB") }), "LTRIM(    LiteDB)");
        yield return (BsonExpression.Call(BsonExpression.GetMethod("RTRIM", 1), new BsonExpression[] { BsonExpression.Constant("LiteDB    ") }), "RTRIM(LiteDB    )");
        yield return (BsonExpression.Call(BsonExpression.GetMethod("TRIM", 1), new BsonExpression[] { BsonExpression.Constant("    LiteDB    ") }), "TRIM(    LiteDB    )");
        yield return (BsonExpression.Call(BsonExpression.GetMethod("INDEXOF", 2), new BsonExpression[] { BsonExpression.Constant("LiteDB"), BsonExpression.Constant("D") }), "INDEXOF(LiteDB,D)");
        yield return (BsonExpression.Call(BsonExpression.GetMethod("INDEXOF", 3), new BsonExpression[] { BsonExpression.Constant("LiteDB-LiteDB"), BsonExpression.Constant("D"), BsonExpression.Constant(5) }), "INDEXOF(LiteDB-LiteDB,D,5)");
        yield return (BsonExpression.Call(BsonExpression.GetMethod("SUBSTRING", 2), new BsonExpression[] { BsonExpression.Constant("LiteDB-LiteDB"), BsonExpression.Constant(4) }), "SUBSTRING(LiteDB-LiteDB,4)");
        yield return (BsonExpression.Call(BsonExpression.GetMethod("SUBSTRING", 3), new BsonExpression[] { BsonExpression.Constant("LiteDB-LiteDB"), BsonExpression.Constant(4), BsonExpression.Constant(2) }), "SUBSTRING(LiteDB-LiteDB,4,2)");
        yield return (BsonExpression.Call(BsonExpression.GetMethod("REPLACE", 3), new BsonExpression[] { BsonExpression.Constant("LiteDB"), BsonExpression.Constant("t"), BsonExpression.Constant("v") }), "REPLACE(LiteDB,t,v)");
        yield return (BsonExpression.Call(BsonExpression.GetMethod("LPAD", 3), new BsonExpression[] { BsonExpression.Constant("LiteDB"), BsonExpression.Constant(10), BsonExpression.Constant("-") }), "LPAD(LiteDB,10,-)");
        yield return (BsonExpression.Call(BsonExpression.GetMethod("RPAD", 3), new BsonExpression[] { BsonExpression.Constant("LiteDB"), BsonExpression.Constant(10), BsonExpression.Constant("-") }), "RPAD(LiteDB,10,-)");
        yield return (BsonExpression.Call(BsonExpression.GetMethod("SPLIT", 2), new BsonExpression[] { BsonExpression.Constant("LiteDB-LiteDB"), BsonExpression.Constant("-") }), "SPLIT(LiteDB-LiteDB,-)");
        yield return (BsonExpression.Call(BsonExpression.GetMethod("SPLIT", 3), new BsonExpression[] { BsonExpression.Constant("LiteDB-LiteDB"), BsonExpression.Constant("(-)"), BsonExpression.Constant(true) }), "SPLIT(LiteDB-LiteDB,(-),True)");
        yield return (BsonExpression.Call(BsonExpression.GetMethod("FORMAT", 2), new BsonExpression[] { BsonExpression.Constant(42), BsonExpression.Constant("X") }), "FORMAT(42,X)");
        yield return (BsonExpression.Call(BsonExpression.GetMethod("JOIN", 1), new BsonExpression[] { BsonExpression.MakeArray(new BsonExpression[] { BsonExpression.Constant("LiteDB"), BsonExpression.Constant("-LiteDB") }) }), "JOIN([LiteDB,-LiteDB])");
        yield return (BsonExpression.Call(BsonExpression.GetMethod("JOIN", 2), new BsonExpression[] { BsonExpression.MakeArray(new BsonExpression[] { BsonExpression.Constant("LiteDB"), BsonExpression.Constant("LiteDB") }), BsonExpression.Constant("/") }), "JOIN([LiteDB,LiteDB],/)");

        yield return (new BinaryBsonExpression(BsonExpressionType.Add, BsonExpression.Constant(1), BsonExpression.Constant(2)), "1+2");
        yield return (new BinaryBsonExpression(BsonExpressionType.Subtract, BsonExpression.Constant(1), BsonExpression.Constant(2)), "1-2");
        yield return (new BinaryBsonExpression(BsonExpressionType.Multiply, BsonExpression.Constant(1), BsonExpression.Constant(2)), "1*2");
        yield return (new BinaryBsonExpression(BsonExpressionType.Divide, BsonExpression.Constant(1), BsonExpression.Constant(2)), "1/2");
        yield return (new BinaryBsonExpression(BsonExpressionType.Modulo, BsonExpression.Constant(1), BsonExpression.Constant(2)), "1%2");
        yield return (new BinaryBsonExpression(BsonExpressionType.Equal, BsonExpression.Constant(1), BsonExpression.Constant(2)), "1=2");
        yield return (new BinaryBsonExpression(BsonExpressionType.NotEqual, BsonExpression.Constant(1), BsonExpression.Constant(2)), "1!=2");
        yield return (new BinaryBsonExpression(BsonExpressionType.GreaterThan, BsonExpression.Constant(1), BsonExpression.Constant(2)), "1>2");
        yield return (new BinaryBsonExpression(BsonExpressionType.GreaterThanOrEqual, BsonExpression.Constant(1), BsonExpression.Constant(2)), "1>=2");
        yield return (new BinaryBsonExpression(BsonExpressionType.LessThan, BsonExpression.Constant(1), BsonExpression.Constant(2)), "1<2");
        yield return (new BinaryBsonExpression(BsonExpressionType.LessThanOrEqual, BsonExpression.Constant(1), BsonExpression.Constant(2)), "1<=2");
        yield return (new BinaryBsonExpression(BsonExpressionType.Contains, BsonExpression.MakeArray(new BsonExpression[] { BsonExpression.Constant(1), BsonExpression.Constant(2) }), BsonExpression.Constant(3)), "[1,2] CONTAINS 3");
        yield return (new BinaryBsonExpression(BsonExpressionType.Between, BsonExpression.Constant(1), BsonExpression.MakeArray(new BsonExpression[] { BsonExpression.Constant(2), BsonExpression.Constant(3) })), "1 BETWEEN 2 AND 3");
        yield return (new BinaryBsonExpression(BsonExpressionType.Like, BsonExpression.Constant("LiteDB"), BsonExpression.Constant("L%")), "LiteDB LIKE L%");
        yield return (new BinaryBsonExpression(BsonExpressionType.In, BsonExpression.Constant(1), BsonExpression.MakeArray(new BsonExpression[] { BsonExpression.Constant(2), BsonExpression.Constant(3) })), "1 IN [2,3]");
        yield return (new BinaryBsonExpression(BsonExpressionType.Or, BsonExpression.Constant(true), BsonExpression.Constant(false)), "True OR False");
        yield return (new BinaryBsonExpression(BsonExpressionType.And, BsonExpression.Constant(true), BsonExpression.Constant(false)), "True AND False");
    }

    [Fact]
    public void ToString_Theory()
    {
        foreach (var T in Get_ToStringExpressions())
        {
            var res = T.expression.ToString();
            res.Should().Be(T.result);
        }
    }

    [Theory]
    #region InlineData
    [InlineData("21", BsonExpressionType.Constant, false)]
    [InlineData("2.6", BsonExpressionType.Constant, false)]
    [InlineData("'string'", BsonExpressionType.Constant, false)]
    [InlineData("2+1", BsonExpressionType.Add, false)]
    [InlineData("2-1", BsonExpressionType.Subtract, false)]
    [InlineData("2*1", BsonExpressionType.Multiply, false)]
    [InlineData("2/1", BsonExpressionType.Divide, false)]
    [InlineData("[1,2,3]", BsonExpressionType.Array, false)]
    [InlineData("1=1", BsonExpressionType.Equal, true)]
    [InlineData("2!=1", BsonExpressionType.NotEqual, true)]
    [InlineData("2>1", BsonExpressionType.GreaterThan, true)]
    [InlineData("2>=1", BsonExpressionType.GreaterThanOrEqual, true)]
    [InlineData("1<2", BsonExpressionType.LessThan, true)]
    [InlineData("1<=2", BsonExpressionType.LessThanOrEqual, true)]
    [InlineData("@p0", BsonExpressionType.Parameter, false)]
    [InlineData("UPPER(@p0)", BsonExpressionType.Call, false)]
    [InlineData("'LiteDB' LIKE 'L%'", BsonExpressionType.Like, true)]
    [InlineData("7 BETWEEN 4 AND 10", BsonExpressionType.Between, true)]
    [InlineData("7 IN [1,4,7]", BsonExpressionType.In, true)]
    [InlineData("true AND true", BsonExpressionType.And, false)]
    [InlineData("true OR false", BsonExpressionType.Or, false)]
    [InlineData("arr=>@", BsonExpressionType.Map, false)]
    #endregion
    public void BsonExpressionTypeANDIsPredicate_Theory(string exp, BsonExpressionType type, bool isPredicate)
    {
        var expr = BsonExpression.Create(exp);
        expr.Type.Should().Be(type);
        expr.IsPredicate.Should().Be(isPredicate);
    }




    //OLD
    public static IEnumerable<(string expression, BsonValue result)> Get_MethodCalls()
    {
        yield return ("123", 123);
        yield return ("2.9", 2.9);
        yield return ("null", BsonValue.Null);
        yield return ("true", true);
        yield return ("false", false);
        yield return ("\"string\"", "string");
        yield return ("[]", new BsonArray());
        yield return ("{a:1}", new BsonDocument { ["a"] = 1 });
        yield return ("{a:true,i:0}", new BsonDocument { ["a"] = true, ["i"] = 0 });

        yield return ("12+14", 26);
        yield return ("2.9+3", 5.9);
        yield return ("'Lite'+'DB'", "LiteDB");
        yield return ("12+'string'", "12string");
        yield return ("{a:1}+'string'", "{a:1}string");

        yield return ("name", "Name Surname");
        yield return ("arr[1]", 2);
        yield return ("doc.arr[1]", 11);
        yield return ("arr[@<0]", new BsonArray() { 1, 2, 3 });
        yield return ("arr=>1", new BsonArray() { 1, 1, 1 });
        yield return ("LOWER('abcDE')", "abcde");
        yield return ("UPPER('abcDE')", "ABCDE");
        yield return ("LTRIM('    abcDE')", "abcDE");
        yield return ("RTRIM('abcDE    ')", "abcDE");
        yield return ("TRIM('    abcDE    ')", "abcDE");
        yield return ("INDEXOF('abc','b')", 1);
        yield return ("INDEXOF('abc-abc','b',2)", 5);
        yield return ("SUBSTRING('abc-abc',2)", "c-abc");
        yield return ("SUBSTRING('abc-abc',2,3)", "c-a");
        yield return ("REPLACE('abc-abc','a','E')", "Ebc-Ebc");
        yield return ("LPAD('abc',7,'-')", "----abc");
        yield return ("RPAD('abc',7,'-')", "abc----");
        yield return ("SPLIT('abc-abc-abc','-')", new BsonArray() { "abc", "abc", "abc" });
        yield return ("SPLIT('abc-abc-abc','(-)', true)", new BsonArray() { "abc", "-", "abc", "-", "abc" });
        yield return ("FORMAT(42,'X')", "2A");
        yield return ("JOIN(['abc','-abc'])", "abc-abc");
        yield return ("JOIN(['abc','abc'],'/')", "abc/abc");

    }

    [Fact]
    public void Create_MethodCalls()
    {
        foreach (var T in Get_MethodCalls())
        {
            BsonExpression.Create(T.expression).Execute(doc).Should().Be(T.result);
        }
    }
    //OLD\
    /*public static IEnumerable<object[]> Get_MethodCalls()
    {
        yield return new object[] { "123", 123};
        yield return new object[] {"2.9", 2.9 };
        yield return new object[] {"null", BsonValue.Null };
        yield return new object[] {"true", true };
        yield return new object[] {"false", false };
        yield return new object[] {"\"string\"", "string" };
        yield return new object[] {"[]", new BsonArray() };
        yield return new object[] {"{a:1}", new BsonDocument { ["a"] = 1 } };
        yield return new object[] {"{a:true,i:0}", new BsonDocument { ["a"] = true, ["i"] = 0 } };

        yield return new object[] {"12+14", 26 };
        yield return new object[] {"2.9+3", 5.9 };
        yield return new object[] {"'Lite'+'DB'", "LiteDB" };
        yield return new object[] {"12+'string'", "12string" };
        yield return new object[] {"{a:1}+'string'", "{a:1}string" };
        //yield return (BsonExpression.Add(BsonExpression.Constant(1), "string"), null);

        //yield return ("'10/01/2003'+'string'", null);

        yield return new object[] { "name", "Name Surname" };
        yield return new object[] {"arr[1]", 2 };
        yield return new object[] {"doc.arr[1]", 11 };
        yield return new object[] {"arr[@<0]", new BsonArray() { 1, 2, 3 } };
        yield return new object[] {"arr=>1", new BsonArray() {1,1,1} };
        yield return new object[] {"LOWER('abcDE')", "abcde" };
        yield return new object[] {"UPPER('abcDE')", "ABCDE" };
        yield return new object[] {"LTRIM('    abcDE')", "abcDE" };
        yield return new object[] {"RTRIM('abcDE    ')", "abcDE" };
        yield return new object[] {"TRIM('    abcDE    ')", "abcDE" };
        yield return new object[] {"INDEXOF('abc','b')", 1 };
        yield return new object[] {"INDEXOF('abc-abc','b',2)", 5 };
        yield return new object[] {"SUBSTRING('abc-abc',2)", "c-abc" };
        yield return new object[] {"SUBSTRING('abc-abc',2,3)", "c-a" };
        yield return new object[] {"REPLACE('abc-abc','a','E')", "Ebc-Ebc" };
        yield return new object[] {"LPAD('abc',7,'-')", "----abc" };
        yield return new object[] {"RPAD('abc',7,'-')", "abc----" };
        yield return new object[] {"SPLIT('abc-abc-abc','-')", new BsonArray() {"abc","abc","abc"} };
        yield return new object[] {"SPLIT('abc-abc-abc','(-)', true)", new BsonArray() { "abc", "-", "abc", "-", "abc" } };
        yield return new object[] {"FORMAT(42,'X')", "2A" };
        yield return new object[] {"JOIN(['abc','-abc'])", "abc-abc" };
        yield return new object[] {"JOIN(['abc','abc'],'/')", "abc/abc" };

    }

    [Theory]
    [MemberData(nameof(Get_MethodCalls))]
    public void Execute_MethodCalls(params object[] calls)
    {
        BsonValue bson = calls[1] as BsonValue;
        BsonExpression.Create(calls[0].As<string>()).Execute(doc).Should().Be(bson);
    }*/
}