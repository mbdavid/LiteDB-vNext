using Bogus.Bson;
using Castle.Core.Configuration;
using Newtonsoft.Json.Linq;
using static LiteDB.BsonExpression;

namespace LiteDB.Tests.Expressions;


public class BsonExpressions_Parser_Tests
{
    private static BsonExpression Array(params BsonValue[] values)
    {
        var expressions = new List<BsonExpression>();
        foreach (BsonValue value in values)
        {
            expressions.Add(Constant(value));
        }
        return MakeArray(expressions);
    }



    public static IEnumerable<object[]> Get_Methods()
    {
        yield return new object[] { "$.clients", new PathBsonExpression(Root(), "clients") };
        yield return new object[] { "$.doc.arr", new PathBsonExpression(new PathBsonExpression(Root(), "doc"), "arr") };
        yield return new object[] { "@.name", new PathBsonExpression(Current(), "name") };
        yield return new object[] { "$.clients[age>=18]", new FilterBsonExpression(new PathBsonExpression(Root(), "clients"), new BinaryBsonExpression(BsonExpressionType.GreaterThanOrEqual, new PathBsonExpression(Current(), "age"), Constant(18)))};
        yield return new object[] { "$.clients=>@.name", new MapBsonExpression(new PathBsonExpression(Root(), "clients"), new PathBsonExpression(Current(), "name")) };
        yield return new object[] { "$.arr[1]", new ArrayIndexBsonExpression(new PathBsonExpression(Root(), "arr"), Constant(1))};

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

        yield return new object[] { "LOWER(\"LiteDB\")"                    , Call(GetMethod("LOWER", 1), new BsonExpression[] { Constant("LiteDB") })                                                               };
        yield return new object[] { "UPPER(\"LiteDB\")"                    , Call(GetMethod("UPPER", 1), new BsonExpression[] { Constant("LiteDB") })                                                               };
        yield return new object[] { "LTRIM(\"    LiteDB\")"                , Call(GetMethod("LTRIM", 1), new BsonExpression[] { Constant("    LiteDB") })                                                           };
        yield return new object[] { "RTRIM(\"LiteDB    \")"                , Call(GetMethod("RTRIM", 1), new BsonExpression[] { Constant("LiteDB    ") })                                                           };
        yield return new object[] { "TRIM(\"    LiteDB    \")"             , Call(GetMethod("TRIM", 1), new BsonExpression[] { Constant("    LiteDB    ") })                                                        };
        yield return new object[] { "INDEXOF(\"LiteDB\",\"D\")"            , Call(GetMethod("INDEXOF", 2), new BsonExpression[] { Constant("LiteDB"), Constant("D") })                                              };
        yield return new object[] { "INDEXOF(\"LiteDB-LiteDB\",\"D\",5)"   , Call(GetMethod("INDEXOF", 3), new BsonExpression[] { Constant("LiteDB-LiteDB"), Constant("D"), Constant(5) })                          };
        yield return new object[] { "SUBSTRING(\"LiteDB-LiteDB\",4)"       , Call(GetMethod("SUBSTRING", 2), new BsonExpression[] { Constant("LiteDB-LiteDB"), Constant(4) })                                       };
        yield return new object[] { "SUBSTRING(\"LiteDB-LiteDB\",4,2)"     , Call(GetMethod("SUBSTRING", 3), new BsonExpression[] { Constant("LiteDB-LiteDB"), Constant(4), Constant(2) })                          };
        yield return new object[] { "REPLACE(\"LiteDB\",\"t\",\"v\")"      , Call(GetMethod("REPLACE", 3), new BsonExpression[] { Constant("LiteDB"), Constant("t"), Constant("v") })                               };
        yield return new object[] { "LPAD(\"LiteDB\",10,\"-\")"            , Call(GetMethod("LPAD", 3), new BsonExpression[] { Constant("LiteDB"), Constant(10), Constant("-") })                                   };
        yield return new object[] { "RPAD(\"LiteDB\",10,\"-\")"            , Call(GetMethod("RPAD", 3), new BsonExpression[] { Constant("LiteDB"), Constant(10), Constant("-") })                                   };
        yield return new object[] { "SPLIT(\"LiteDB-LiteDB\",\"-\")"       , Call(GetMethod("SPLIT", 2), new BsonExpression[] { Constant("LiteDB-LiteDB"), Constant("-") })                                         };
        yield return new object[] { "SPLIT(\"LiteDB-LiteDB\",\"(-)\",true)", Call(GetMethod("SPLIT", 3), new BsonExpression[] { Constant("LiteDB-LiteDB"), Constant("(-)"), Constant(true) })                       };
        yield return new object[] { "FORMAT(42,\"X\")"                     , Call(GetMethod("FORMAT", 2), new BsonExpression[] { Constant(42), Constant("X") })                                                     };
        yield return new object[] { "JOIN([\"LiteDB\",\"-LiteDB\"])"       , Call(GetMethod("JOIN", 1), new BsonExpression[] { Array("LiteDB", "-LiteDB") })                                                        };
        yield return new object[] { "JOIN([\"LiteDB\",\"LiteDB\"],\"/\")"  , Call(GetMethod("JOIN", 2), new BsonExpression[] { Array("LiteDB", "LiteDB"), Constant("/") })                                          };


    }

    [Theory]
    [MemberData(nameof(Get_Methods))]
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