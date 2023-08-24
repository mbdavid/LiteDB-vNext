using System.Data.SqlTypes;

namespace LiteDB;

#region TokenType definition

/// <summary>
/// ASCII char names: https://www.ascii.cl/htmlcodes.htm
/// </summary>
internal enum TokenType
{
    /// <summary> { </summary>
    OpenBrace,
    /// <summary> } </summary>
    CloseBrace,
    /// <summary> [ </summary>
    OpenBracket,
    /// <summary> ] </summary>
    CloseBracket,
    /// <summary> ( </summary>
    OpenParenthesis,
    /// <summary> ) </summary>
    CloseParenthesis,
    /// <summary> , </summary>
    Comma,
    /// <summary> : </summary>
    Colon,
    /// <summary> ; </summary>
    SemiColon,
    /// <summary> =&gt; </summary>
    Arrow,
    /// <summary> @ </summary>
    At,
    /// <summary> # </summary>
    Hashtag,
    /// <summary> ~ </summary>
    Til,
    /// <summary> . </summary>
    Period,
    /// <summary> &amp; </summary>
    Ampersand,
    /// <summary> $ </summary>
    Dollar,
    /// <summary> ! </summary>
    Exclamation,
    /// <summary> != </summary>
    NotEquals,
    /// <summary> = </summary>
    Equals,
    /// <summary> &gt; </summary>
    Greater,
    /// <summary> &gt;= </summary>
    GreaterOrEquals,
    /// <summary> &lt; </summary>
    Less,
    /// <summary> &lt;= </summary>
    LessOrEquals,
    /// <summary> - </summary>
    Minus,
    /// <summary> + </summary>
    Plus,
    /// <summary> * </summary>
    Asterisk,
    /// <summary> / </summary>
    Slash,
    /// <summary> \ </summary>
    Backslash,
    /// <summary> % </summary>
    Percent,
    /// <summary> "..." or '...' </summary>
    String,
    /// <summary> [0-9]+ </summary>
    Int,
    /// <summary> [0-9]+.[0-9] </summary>
    Double,
    /// <summary> \n\r\t \u0032 </summary>
    Whitespace,
    /// <summary> [a-Z_$]+[a-Z0-9_$] </summary>
    Word,
    EOF,
    Unknown
}

#endregion

#region Token definition

/// <summary>
/// Represent a single string token
/// </summary>
internal struct Token : INullable
{
    public Token(TokenType tokenType, string value, long position)
    {
        this.Position = position;
        this.Value = value;
        this.Type = tokenType;
    }

    public TokenType Type { get; private set; }
    public string Value { get; private set; }
    public long Position { get; private set; }

    public bool IsNull => this.Value==string.Empty;

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

/// <summary>
/// Class to tokenize TextReader input used in JsonRead/BsonExpressions
/// This class are not thread safe
/// </summary>
internal class Tokenizer
{
    private string _reader;
    private char _char = '\0';
    private Token _current = new Token(TokenType.Unknown, "", 0);
    private Token _ahead = new Token(TokenType.Unknown, "", 0);
    private bool _eof = false;
    private int _position = 0;

    public bool EOF => _eof && _ahead.IsNull;
    public long Position => _position;
    public Token Current => _current!;

    /// <summary>
    /// If EOF throw an invalid token exception (used in while()) otherwise return "false" (not EOF)
    /// </summary>
    public bool CheckEOF()
    {
        if (_eof) throw ERR_UNEXPECTED_TOKEN(this.Current);

        return false;
    }

    public Tokenizer(string reader)
    {
        _reader = reader;
        _position = 0;
        this.ReadChar();
    }

    /// <summary>
    /// Checks if char is an valid part of a word [a-Z_]+[a-Z0-9_$]*
    /// </summary>
    public static bool IsWordChar(char c, bool first)
    {
        if (first)
        {
            return char.IsLetter(c) || c == '_' || c == '$';
        }

        return char.IsLetterOrDigit(c) || c == '_' || c == '$';
    }

    /// <summary>
    /// Read next char in stream and set in _current
    /// </summary>
    private char ReadChar()
    {
        if (_position >= _reader.Length || _eof) return '\0';

        var c = _reader[_position];

        _position++;

        if (c == -1)
        {
            _char = '\0';
            _eof = true;
        }
        else
        {
            _char = (char)c;
        }

        return _char;
    }

    /// <summary>
    /// Look for next token but keeps in buffer when run "ReadToken()" again.
    /// </summary>
    public Token LookAhead(bool eatWhitespace = true, int tokensCount = 1)
    {
        if (tokensCount <= 0)
            return _current;
        if(tokensCount==1)
        {
            if (_ahead.IsNull == false)
            {
                if (eatWhitespace && _ahead.Type == TokenType.Whitespace)
                {
                    _ahead = this.ReadNext(eatWhitespace);
                }

                return _ahead;
            }

            return _ahead = this.ReadNext(eatWhitespace);
        }
        else
        {
            var keepPos = _position;
            var keepCurent = _current;
            var keepAhead = this.ReadNext(eatWhitespace);
            for (int i=1; i<tokensCount-1; i++)
                this.ReadNext(eatWhitespace);
            var tok = this.ReadNext(eatWhitespace);
            _position = keepPos;
            this.ReadChar();
            _ahead = keepAhead;
            _current = keepCurent;
            return tok;
        }
    }

    /// <summary>
    /// Read next token (or from ahead buffer).
    /// </summary>
    public Token ReadToken(bool eatWhitespace = true)
    {
        if (_ahead.IsNull)
        {
            return _current = this.ReadNext(eatWhitespace);
        }

        if (eatWhitespace && _ahead.Type == TokenType.Whitespace)
        {
            _ahead = this.ReadNext(eatWhitespace);
        }

        _current = _ahead;
        _ahead = new Token(TokenType.Unknown, string.Empty, 0);
        return _current;
    }

    /// <summary>
    /// Read next token from reader
    /// </summary>
    private Token ReadNext(bool eatWhitespace)
    {
        // remove whitespace before get next token
        if (eatWhitespace) this.EatWhitespace();

        if (_eof)
        {
            return new Token(TokenType.EOF, "", _position);
        }

        Token token = new Token(TokenType.Unknown, _char.ToString(), _position);

        switch (_char)
        {
            case '{':
                token = new Token(TokenType.OpenBrace, "{", _position);
                this.ReadChar();
                break;

            case '}':
                token = new Token(TokenType.CloseBrace, "}", _position);
                this.ReadChar();
                break;

            case '[':
                token = new Token(TokenType.OpenBracket, "[", _position);
                this.ReadChar();
                break;

            case ']':
                token = new Token(TokenType.CloseBracket, "]", _position);
                this.ReadChar();
                break;

            case '(':
                token = new Token(TokenType.OpenParenthesis, "(", _position);
                this.ReadChar();
                break;

            case ')':
                token = new Token(TokenType.CloseParenthesis, ")", _position);
                this.ReadChar();
                break;

            case ',':
                token = new Token(TokenType.Comma, ",", _position);
                this.ReadChar();
                break;

            case ':':
                token = new Token(TokenType.Colon, ":", _position);
                this.ReadChar();
                break;

            case ';':
                token = new Token(TokenType.SemiColon, ";", _position);
                this.ReadChar();
                break;

            case '@':
                token = new Token(TokenType.At, "@", _position);
                this.ReadChar();
                break;

            case '#':
                token = new Token(TokenType.Hashtag, "#", _position);
                this.ReadChar();
                break;

            case '~':
                token = new Token(TokenType.Til, "~", _position);
                this.ReadChar();
                break;

            case '.':
                token = new Token(TokenType.Period, ".", _position);
                this.ReadChar();
                break;

            case '&':
                token = new Token(TokenType.Ampersand, "&", _position);
                this.ReadChar();
                break;

            case '$':
                this.ReadChar();
                if (IsWordChar(_char, true))
                {
                    token = new Token(TokenType.Word, "$" + this.ReadWord(), _position);
                }
                else
                {
                    token = new Token(TokenType.Dollar, "$", _position);
                }
                break;

            case '!':
                this.ReadChar();
                if (_char == '=')
                {
                    token = new Token(TokenType.NotEquals, "!=", _position);
                    this.ReadChar();
                }
                else
                {
                    token = new Token(TokenType.Exclamation, "!", _position);
                }
                break;

            case '=':
                this.ReadChar();
                if (_char == '>')
                {
                    token = new Token(TokenType.Arrow, "=>", _position);
                    this.ReadChar();
                }
                else
                {
                    token = new Token(TokenType.Equals, "=", _position);
                }
                break;

            case '>':
                this.ReadChar();
                if (_char == '=')
                {
                    token = new Token(TokenType.GreaterOrEquals, ">=", _position);
                    this.ReadChar();
                }
                else
                {
                    token = new Token(TokenType.Greater, ">", _position);
                }
                break;

            case '<':
                this.ReadChar();
                if (_char == '=')
                {
                    token = new Token(TokenType.LessOrEquals, "<=", _position);
                    this.ReadChar();
                }
                else
                {
                    token = new Token(TokenType.Less, "<", _position);
                }
                break;

            case '-':
                this.ReadChar();
                if (_char == '-')
                {
                    this.ReadLine(); // comment
                    token = this.ReadNext(eatWhitespace);
                }
                else
                {
                    token = new Token(TokenType.Minus, "-", _position);
                }
                break;

            case '+':
                token = new Token(TokenType.Plus, "+", _position);
                this.ReadChar();
                break;

            case '*':
                token = new Token(TokenType.Asterisk, "*", _position);
                this.ReadChar();
                break;

            case '/':
                token = new Token(TokenType.Slash, "/", _position);
                this.ReadChar();
                break;
            case '\\':
                token = new Token(TokenType.Backslash, @"\", _position);
                this.ReadChar();
                break;

            case '%':
                token = new Token(TokenType.Percent, "%", _position);
                this.ReadChar();
                break;

            case '\"':
            case '\'':
                token = new Token(TokenType.String, this.ReadString(_char), _position);
                break;

            case '0':
            case '1':
            case '2':
            case '3':
            case '4':
            case '5':
            case '6':
            case '7':
            case '8':
            case '9':
                var dbl = false;
                var number = this.ReadNumber(ref dbl);
                token = new Token(dbl ? TokenType.Double : TokenType.Int, number, _position);
                break;

            case ' ':
            case '\n':
            case '\r':
            case '\t':
                var sb = new StringBuilder();
                while(char.IsWhiteSpace(_char) && !_eof)
                {
                    sb.Append(_char);
                    this.ReadChar();
                }
                token = new Token(TokenType.Whitespace, sb.ToString(), _position);
                break;

            default:
                // test if first char is an word 
                if (IsWordChar(_char, true))
                {
                    token = new Token(TokenType.Word, this.ReadWord(), _position);
                }
                else
                {
                    this.ReadChar();
                }
                break;
        }

        return token;
    }

    /// <summary>
    /// Eat all whitespace - used before a valid token
    /// </summary>
    private void EatWhitespace()
    {
        while (char.IsWhiteSpace(_char) && !_eof)
        {
            this.ReadChar();
        }
    }

    /// <summary>
    /// Read a word (word = [\w$]+)
    /// </summary>
    private string ReadWord()
    {
        var sb = new StringBuilder();
        sb.Append(_char);

        this.ReadChar();

        while (!_eof && IsWordChar(_char, false))
        {
            sb.Append(_char);
            this.ReadChar();
        }

        return sb.ToString();
    }

    /// <summary>
    /// Read a number - it's accepts all number char, but not validate. When run Convert, .NET will check if number is correct
    /// </summary>
    private string ReadNumber(ref bool dbl)
    {
        var sb = new StringBuilder();
        sb.Append(_char);

        var canDot = true;
        var canE = true;
        var canSign = false;

        this.ReadChar();

        while (!_eof &&
            (char.IsDigit(_char) || _char == '+' || _char == '-' || _char == '.' || _char == 'e' || _char == 'E'))
        {
            if (_char == '.')
            {
                if (canDot == false) break;
                dbl = true;
                canDot = false;
            }
            else if (_char == 'e' || _char == 'E')
            {
                if (canE == false) break;
                canE = false;
                canSign = true;
                dbl = true;
            }
            else if (_char == '-' || _char == '+')
            {
                if (canSign == false) break;
                canSign = false;
            }

            sb.Append(_char);
            this.ReadChar();
        }

        return sb.ToString();
    }
        
    /// <summary>
    /// Read a string removing open and close " or '
    /// </summary>
    private string ReadString(char quote)
    {
        var sb = new StringBuilder();
        this.ReadChar(); // remove first " or '

        while (_char != quote && !_eof)
        {
            if (_char == '\\')
            {
                this.ReadChar();

                if (_char == quote) sb.Append(quote);

                switch (_char)
                {
                    case '\\': sb.Append('\\'); break;
                    case '/': sb.Append('/'); break;
                    case 'b': sb.Append('\b'); break;
                    case 'f': sb.Append('\f'); break;
                    case 'n': sb.Append('\n'); break;
                    case 'r': sb.Append('\r'); break;
                    case 't': sb.Append('\t'); break;
                    case 'u':
                        var codePoint = ParseUnicode(this.ReadChar(), this.ReadChar(), this.ReadChar(), this.ReadChar());
                        sb.Append((char)codePoint);
                        break;
                }
            }
            else
            {
                sb.Append(_char);
            }

            this.ReadChar();
        }

        this.ReadChar(); // read last " or '

        return sb.ToString();
    }

    /// <summary>
    /// Read all chars to end of LINE
    /// </summary>
    private void ReadLine()
    {
        // remove all char until new line
        while (_char != '\n' && !_eof)
        {
            this.ReadChar();
        }
        if (_char == '\n') this.ReadChar();
    }

    public static uint ParseUnicode(char c1, char c2, char c3, char c4)
    {
        uint p1 = ParseSingleChar(c1, 0x1000);
        uint p2 = ParseSingleChar(c2, 0x100);
        uint p3 = ParseSingleChar(c3, 0x10);
        uint p4 = ParseSingleChar(c4, 1);

        return p1 + p2 + p3 + p4;
    }

    public static uint ParseSingleChar(char c1, uint multiplier)
    {
        uint p1 = 0;
        if (c1 >= '0' && c1 <= '9')
            p1 = (uint)(c1 - '0') * multiplier;
        else if (c1 >= 'A' && c1 <= 'F')
            p1 = (uint)((c1 - 'A') + 10) * multiplier;
        else if (c1 >= 'a' && c1 <= 'f')
            p1 = (uint)((c1 - 'a') + 10) * multiplier;
        return p1;
    }

    public override string ToString()
    {
        return _current.ToString() + " [ahead: " + _ahead.ToString() + "] - position: " + _position;
    }
}
