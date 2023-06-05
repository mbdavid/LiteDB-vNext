namespace LiteDB;

/// <summary>
/// Represent a single string token
/// </summary>
internal class Token2
{
    public Token2(TokenType2 tokenType, string value, long position)
    {
        this.Position = position;
        this.Value = value;
        this.Type = tokenType;
    }

    public TokenType2 Type { get; private set; }
    public string Value { get; private set; }
    public long Position { get; private set; }

    /// <summary>
    /// Expect if token is type (if not, throw UnexpectedToken)
    /// </summary>
    public Token2 Expect(TokenType2 type)
    {
        if (this.Type != type)
        {
            throw ERR_UNEXPECTED_TOKEN(this);
        }

        return this;
    }

    /// <summary>
    /// Expect for type1 OR type2 (if not, throw UnexpectedToken)
    /// </summary>
    public Token2 Expect(TokenType2 type1, TokenType2 type2)
    {
        if (this.Type != type1 && this.Type != type2)
        {
            throw ERR_UNEXPECTED_TOKEN(this);
        }

        return this;
    }

    /// <summary>
    /// Expect for type1 OR type2 OR type3 (if not, throw UnexpectedToken)
    /// </summary>
    public Token2 Expect(TokenType2 type1, TokenType2 type2, TokenType2 type3)
    {
        if (this.Type != type1 && this.Type != type2 && this.Type != type3)
        {
            throw ERR_UNEXPECTED_TOKEN(this);
        }

        return this;
    }

    public Token2 Expect(string value, bool ignoreCase = true)
    {
        if (!this.Is(value, ignoreCase))
        {
            throw ERR_UNEXPECTED_TOKEN(this, value);
        }

        return this;
    }

    public bool Is(string value, bool ignoreCase = true)
    {
        return 
            this.Type == TokenType2.Word &&
            value.Equals(this.Value, ignoreCase ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal);
    }

    public override string ToString()
    {
        return this.Value + " (" + this.Type + ")";
    }
}