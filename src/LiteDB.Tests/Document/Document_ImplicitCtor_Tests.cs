namespace LiteDB.Tests.Document;

public class Document_ImplicitCtor_Tests
{
    public BsonType ReturnType(BsonValue value)
    {
        return value.Type;
    }

    public IEnumerable<object[]> GetValues()
    {
        yield return new object[] { null, BsonType.Null };
        yield return new object[] { 10, BsonType.Int32 };
        yield return new object[] { 2147483648, BsonType.Int64 };
        yield return new object[] { 2.6, BsonType.Double };
    }

    #region Numeric

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

    #endregion

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
