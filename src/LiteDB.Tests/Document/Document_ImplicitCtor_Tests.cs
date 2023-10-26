namespace LiteDB.Tests.Document;

public class Document_ImplicitCtor_Tests
{
    public BsonType ReturnType(BsonValue value)
    {
        return value.Type;
    }

    [Fact]
    public void BsonInt32_ImplicitCtor()
    {
        ReturnType(10).Should().Be(BsonType.Int32);
    }

    [Fact]
    public void BsonInt64_ImplicitCtor()
    {
        ReturnType(2147483648l).Should().Be(BsonType.Int64);
    }

    [Fact]
    public void BsonDouble_ImplicitCtor()
    {
        ReturnType(2.6).Should().Be(BsonType.Double);
    }

    [Fact]
    public void BsonDecimal_ImplicitCtor()
    {
        ReturnType(10m).Should().Be(BsonType.Decimal);
    }

    [Fact]
    public void BsonString_ImplicitCtor()
    {
        ReturnType("LiteDB").Should().Be(BsonType.String);
    }

    [Fact]
    public void BsonGuid_ImplicitCtor()
    {
        ReturnType(Guid.NewGuid()).Should().Be(BsonType.Guid);
    }

    [Fact]
    public void BsonDateTime_ImplicitCtor()
    {
        ReturnType(DateTime.Now).Should().Be(BsonType.DateTime);
    }

    [Fact]
    public void BsonBoolean_ImplicitCtor()
    {
        ReturnType(true).Should().Be(BsonType.Boolean);
    }

}
