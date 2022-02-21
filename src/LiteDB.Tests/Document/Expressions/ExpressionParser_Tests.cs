namespace LiteDB.Tests.Expressions;

public class ExpressionParser_Tests
{
    public static BsonValue J(string json) => JsonSerializer.Deserialize(json);

    [Fact]
    public void Expressions_Constants()
    {
        BsonValue K(string s)
        {
            return BsonExpression.Create(s).Execute();
        }

        var e = BsonExpression.Create("a + b.c + items[0].no");

        K(@"123").Should().Be(123);
        K(@"null").Should().Be(BsonValue.Null);
        K(@"15.9").Should().Be(15.9);
        K(@"true").Should().Be(true);
        K(@"false").Should().Be(false);
        K(@"'my string'").Should().Be("my string");
        K(@"""my string""").Should().Be("my string");


    }

    [Fact]
    public void Expressions_Full()
    {
        BsonValue K(string s)
        {
            return BsonExpression.Create(s).Execute();
        }

        K(@"1 + 1").Should().Be(2);
        K(@"1 = 1").Should().Be(true);
        K(@"5 + 2 * 5 = 15").Should().Be(true);

        K(@"'john' LIKE 'jo%'").Should().Be(true);
        K(@"'john' LIKE 'john%'").Should().Be(true);
        K(@"'john' LIKE 'joe%'").Should().Be(false);
        K(@"'john' LIKE 'J%'").Should().Be(false); // collation = binary by default

        K(@"'a' IN ['b','a']").Should().Be(true);
        K(@"'a' IN [1,2]").Should().Be(false);
        K(@"'b' IN []").Should().Be(false);
        K(@"'b' IN 'b'").Should().Be(false);

        K(@"20 BETWEEN 5 AND 30").Should().Be(true);
        K(@"20 BETWEEN 20 AND 30").Should().Be(true);
        K(@"20 BETWEEN 30 AND 60").Should().Be(false);

        K(@"[1,2,3] CONTAINS 2").Should().Be(true);
        K(@"[1,2,3] CONTAINS 0").Should().Be(false);
        K(@"[1,2,3] CONTAINS (1 + 1)").Should().Be(true);

    }

    [Fact]
    public void Expressions_Parameters()
    {
        BsonValue K(string s)
        {
            return BsonExpression.Create(s).Execute(null, new BsonDocument
            {
                ["0"] = "value0",
                ["demo"] = "value1"
            });
        }

        K(@"@0").Should().Be("value0");
        K(@"@demo").Should().Be("value1");
    }

    [Fact]
    public void Expressions_PathNavigation()
    {
        BsonValue K(string s)
        {
            return BsonExpression.Create(s).Execute(new BsonDocument
            {
                ["_id"] = 1,
                ["name"] = "John",
                ["address"] = new BsonDocument
                {
                    ["street"] = "Av. Ipiranga",
                    ["city"] = "Porto Alegre",
                    ["location"] = new BsonDocument
                    {
                        ["lat"] = 33.11,
                        ["lng"] = 22.11
                    }
                }
            });
        }

        K(@"_id").Should().Be(1);
        K(@"name").Should().Be("John");
        K("$.address.street").Should().Be("Av. Ipiranga");
        K("$.address.location.lat").Should().Be(33.11);

    }

    [Fact]
    public void Expressions_ArrayNavigation()
    {
        BsonValue K(string s)
        {
            return BsonExpression.Create(s).Execute(new BsonDocument
            {
                ["products"] = new BsonArray
                {
                    new BsonDocument { ["price"] = 100, ["name"] = "TV" },
                    new BsonDocument { ["price"] = 500, ["name"] = "DVD" },
                }
            },
            new BsonDocument
            {
                ["p0"] = 1
            });
        }

        K("$.products[0]").Should().Be(new BsonDocument { ["price"] = 100, ["name"] = "TV" });
        K("$.products[-1]").Should().Be(new BsonDocument { ["price"] = 500, ["name"] = "DVD" });
        K("$.products[-1].price").Should().Be(500);

        K("$.notfound").Should().Be(BsonValue.Null);
        K("$.products[99].price").Should().Be(BsonValue.Null);

        K("$.products[price > 200]").Should().Be(new BsonArray { new BsonDocument { ["price"] = 500, ["name"] = "DVD" } });
        K("$.products[price > 1000]").Should().Be(new BsonArray());

        K("$.products[*].price").Should().Be(new BsonArray { 100, 500 });
        K("$.products[price > 150].price").Should().Be(new BsonArray { 500 });

        K("$.products[@p0].price").Should().Be(500);

    }

    [Fact]
    public void Expressions_MakeArray()
    {
        BsonValue K(string s)
        {
            return BsonExpression.Create(s).Execute();
        }

        K("[]").Should().Be(new BsonArray());
        K("[1,2]").Should().Be(new BsonArray { 1, 2 });
        K("[null, ['a', 'b']]").Should().Be(new BsonArray { BsonValue.Null, new BsonArray { "a", "b" } });
    }

    [Fact]
    public void Expressions_MakeDocument()
    {
        BsonValue K(string s)
        {
            return BsonExpression.Create(s).Execute(new BsonDocument
            {
                ["a"] = 1,
                ["b"] = 2,
                ["c"] = new BsonDocument { ["c1"] = 3 }
            });
        }

        K("{}").Should().Be(new BsonDocument());
        K("{a:5}").Should().Be(new BsonDocument { ["a"] = 5 });
        K("{a:5+5}").Should().Be(new BsonDocument { ["a"] = 10 });
        K("{a:5+a}").Should().Be(new BsonDocument { ["a"] = 6 });
        K("{a:5, b:10}").Should().Be(new BsonDocument { ["a"] = 5, ["b"] = 10 });

        // simplified version
        K("{a}").Should().Be(new BsonDocument { ["a"] = 1 });
        K("{b,a}").Should().Be(new BsonDocument { ["a"] = 1, ["b"] = 2 });
    }

    [Fact]
    public void Expressions_Map()
    {
        BsonValue K(string s)
        {
            return BsonExpression.Create(s).Execute(new BsonDocument
            {
                ["items"] = new BsonArray
                {
                    new BsonDocument { ["name"] = "John", ["age"] = 20 },
                    new BsonDocument { ["name"] = "Doe", ["age"] = 40 },
                }
            });
        }
        K("$.items => { name, idade: age - 1 }").Should().Be(J("[{name:'John',idade:19}, {name:'Doe', idade: 39}]"));

        K("$.items => 1").Should().Be(new BsonArray { 1, 1 });
        K("$.items => @.age").Should().Be(new BsonArray { 20, 40 });
        K("$.items => (@.age + 10)").Should().Be(new BsonArray { 30, 50 });
    }

    [Fact]
    public void Expressions_Method()
    {
        BsonValue K(string s)
        {
            return BsonExpression.Create(s).Execute(new BsonDocument
            {
                ["name"] = "david",
                ["age"] = 44,
                ["items"] = new BsonArray(1, 2, 10)
            }, null, new Collation("pt-BR"));
        }

        K("UPPER('john')").Should().Be("JOHN");
        K("DOUBLE('105,99')").Should().Be(105.99); // pt-BR culture
        K("JOIN($.items[*], '-')").Should().Be("1-2-10");
    }

    [Fact]
    public void Expressions_ToString()
    {
        string K(string s)
        {
            return BsonExpression.Create(s).ToString();
        }

        K("_id").Should().Be("$._id");
        K("$._id").Should().Be("$._id");

        K("$.[\"new name\"]").Should().Be("$.[\"new name\"]");

        K("address.street").Should().Be("$.address.street");
        K("address.location.lat").Should().Be("$.address.location.lat");

        K("products[0]").Should().Be("$.products[0]");
        K("products[0].name").Should().Be("$.products[0].name");
        K("products[0].stock.price").Should().Be("$.products[0].stock.price");

        K("products[price > 0]").Should().Be("$.products[@.price>0]");
        K("products[price > 0].price").Should().Be("$.products[@.price>0].price");

        K("products[*]").Should().Be("$.products");

        K("JOIN($.items, '-')").Should().Be("JOIN($.items,\"-\")");
        K("JOIN($.items[*], '-')").Should().Be("JOIN($.items,\"-\")");


    }
}
