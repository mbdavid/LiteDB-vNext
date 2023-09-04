namespace LiteDB.Tests.Expressions;
public class Tokenizer_Tests
{
    public static IEnumerable<object[]> Get_Tokens()
    {
        yield return new object[] { "{", new Token[] { new Token(TokenType.OpenBrace, "{", 0)} };
        yield return new object[] { "alpha", new Token[] { new Token(TokenType.Word, "alpha", 0) } };
        yield return new object[] { "a,b,c", new Token[] { new Token(TokenType.Word, "a", 0), new Token(TokenType.Comma, ",", 0), new Token(TokenType.Word, "b", 0), new Token(TokenType.Comma, ",", 0), new Token(TokenType.Word, "c", 0) } };
    }

    [Theory]
    [MemberData(nameof(Get_Tokens))]
    public void ReadToken_Theory(params object[] T)
    {
        var t = new Tokenizer(T[0].As<string>());
        foreach (var s in T[1].As<Token[]>())
        {
            var token = t.ReadToken();
            token.Value.Should().Be(s.Value);
            token.Type.Should().Be(s.Type);
        }
    }

    public static IEnumerable<object[]> Get_Tokens2()
    {
        yield return new object[] { "{a b c", new Token[] { new Token(TokenType.Word, "b", 0), new Token(TokenType.OpenBrace, "{", 0), new Token(TokenType.Word, "a", 0) }, 3 };
        yield return new object[] { "alpha beta charlie", new Token[] { new Token(TokenType.Word, "beta", 0), new Token(TokenType.Word, "alpha", 0), new Token(TokenType.Word, "beta", 0), new Token(TokenType.Word, "charlie", 0) }, 2 };
        yield return new object[] { "alpha beta charlie", new Token[] { new Token(TokenType.Word, "charlie", 0), new Token(TokenType.Word, "alpha", 0), new Token(TokenType.Word, "beta", 0), new Token(TokenType.Word, "charlie", 0) }, 3 };
        yield return new object[] { "alpha beta charlie", new Token[] { new Token(TokenType.EOF, "", 0), new Token(TokenType.Word, "alpha", 0), new Token(TokenType.Word, "beta", 0), new Token(TokenType.Word, "charlie", 0) }, 4 };
    }

    [Theory]
    [MemberData(nameof(Get_Tokens2))]
    public void LookAhead_Theory(params object[] T)
    {
        var t = new Tokenizer(T[0].As<string>());
        var expected = T[1].As<Token[]>();

        var tok = t.LookAhead(true, T[2].As<int>());
        tok.Value.Should().Be(expected[0].Value);
        tok.Type.Should().Be(expected[0].Type);

        for (int i = 1; i < expected.Length; i++)
        {
            var s = expected[i];
            tok = t.ReadToken();
            tok.Value.Should().Be(s.Value);
            tok.Type.Should().Be(s.Type);
        }
    }
}
