namespace LiteDB;

internal class JsonReaderStatic
{
    private readonly static IFormatProvider _numberFormat = CultureInfo.InvariantCulture.NumberFormat;

    /// <summary>
    /// Deserialize an document using same Tokenizer from BsonExpression
    /// </summary>
    public static BsonDocument Deserialize(Tokenizer tokenizer)
    {
        throw new NotImplementedException();
    }
}
