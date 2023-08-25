using System.Data.SqlTypes;

namespace LiteDB;

#region Token definition

/// <summary>
/// Represent a single string token
/// </summary>
internal struct Token
{
    public Token(TokenType tokenType, string value, long position)
    {
        this.Position = position;
        this.Value = value;
        this.Type = tokenType;
    }

    public static Token Empty = new(TokenType.EOF, "", 0);

    public TokenType Type { get; private set; }
    public string Value { get; private set; }
    public long Position { get; private set; }

    public bool IsEmpty => this.Value == string.Empty && this.Type==TokenType.EOF;

    /// <summary>
    /// Expect if token is type (if not, throw UnexpectedToken)
    /// </summary>
    public Token Expect(TokenType type)
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
    public Token Expect(TokenType type1, TokenType type2)
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
    public Token Expect(TokenType type1, TokenType type2, TokenType type3)
    {
        if (this.Type != type1 && this.Type != type2 && this.Type != type3)
        {
            throw ERR_UNEXPECTED_TOKEN(this);
        }

        return this;
    }

    public Token Expect(string value, bool ignoreCase = true)
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
            this.Type == TokenType.Word &&
            value.Equals(this.Value, ignoreCase ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal);
    }

    public override string ToString()
    {
        return this.Value + " (" + this.Type + ")";
    }
}

#endregion