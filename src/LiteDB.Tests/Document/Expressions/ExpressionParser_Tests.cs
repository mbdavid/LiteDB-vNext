namespace LiteDB.Tests.Expressions;

public class ExpressionParser_Tests
{
    public static BsonDocument J(string json) => JsonSerializer.Deserialize(json).AsDocument;

    [Fact]
    public void Expressions_Constants()
    {
        BsonValue K(string s)
        {
            return BsonExpression.Create(s).Execute();
        }

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
            });
        }

        K("$.products[0]").Should().Be(new BsonDocument { ["price"] = 100, ["name"] = "TV" });
        K("$.products[-1]").Should().Be(new BsonDocument { ["price"] = 500, ["name"] = "DVD" });
        K("$.products[-1].price").Should().Be(500);

        K("$.products[price > 200]").Should().Be(new BsonArray { new BsonDocument { ["price"] = 500, ["name"] = "DVD" } });
        K("$.products[price > 1000]").Should().Be(new BsonArray());

    }

    [Fact]
    public void Expressions_ToString()
    {
        string K(string s)
        {
            return BsonExpression.Create(s).ToString();
        }

        // check source


        K("_id").Should().Be("$._id");
        K("$._id").Should().Be("$._id");

        K("$.[\"new name\"]").Should().Be("$.[\"new name\"]");

        K("address.street").Should().Be("$.address.street");
        K("address.location.lat").Should().Be("$.address.location.lat");

        K("products[0]").Should().Be("$.products[0]");

    }
}
