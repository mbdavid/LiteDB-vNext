namespace LiteDB;
/// <summary>
/// Represent a single string token
/// </summary>
internal class JsonToken
{
    public JsonToken(JsonTokenType tokenType, string value, long position)
    {
        this.Position = position;
        this.Value = value;
        this.Type = tokenType;
    }

    public JsonTokenType Type { get; private set; }
    public string Value { get; private set; }
    public long Position { get; private set; }

    /// <summary>
    /// Expect if token is type (if not, throw UnexpectedToken)
    /// </summary>
    public JsonToken Expect(JsonTokenType type)
    {
        if (this.Type != type)
        {
            throw new Exception();
        }

        return this;
    }

    /// <summary>
    /// Expect for type1 OR type2 (if not, throw UnexpectedToken)
    /// </summary>
    public JsonToken Expect(JsonTokenType type1, JsonTokenType type2)
    {
        if (this.Type != type1 && this.Type != type2)
        {
            throw new Exception();
        }

        return this;
    }

    /// <summary>
    /// Expect for type1 OR type2 OR type3 (if not, throw UnexpectedToken)
    /// </summary>
    public JsonToken Expect(JsonTokenType type1, JsonTokenType type2, JsonTokenType type3)
    {
        if (this.Type != type1 && this.Type != type2 && this.Type != type3)
        {
            throw new Exception();
        }

        return this;
    }

    public JsonToken Expect(string value, bool ignoreCase = true)
    {
        if (!this.Is(value, ignoreCase))
        {
            throw new Exception();
        }

        return this;
    }

    public bool Is(string value, bool ignoreCase = true)
    {
        return
            this.Type == JsonTokenType.Word &&
            value.Equals(this.Value, ignoreCase ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal);
    }

    public override string ToString()
    {
        return this.Value + " (" + this.Type + ")";
    }
}