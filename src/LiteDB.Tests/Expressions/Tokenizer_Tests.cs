using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LiteDB.Tests.Expressions;
public class Tokenizer_Tests
{
    public static IEnumerable<object[]> Get_Tokens()
    {
        yield return new object[] { "{", new Token[] { new Token(TokenType.OpenBrace, "{", 0)} };
        //yield return new object[] { "a,b,c", new Token[] { new Token(TokenType.Word, "a", 2), new Token(TokenType.Comma, ",", 2), new Token(TokenType.Word, "b", 4), new Token(TokenType.Comma, ",", 4), new Token(TokenType.Word, "c", 6) } };
    }

    [Theory]
    [MemberData(nameof(Get_Tokens))]
    public void Test(params object[] T)
    {
        var t = new Tokenizer(T[0].As<string>());
        foreach (var s in T[1].As<Token[]>())
        {
            var token = t.ReadToken();
            token.Value.Should().Be(s.Value);
            token.Type.Should().Be(s.Type);
        }
    }
}
