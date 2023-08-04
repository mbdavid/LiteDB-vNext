using NSubstitute.ExceptionExtensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LiteDB.Tests.Expressions;

public class BsonExpressionParser_Tests
{

    [Theory]
    [InlineData("1 BETWEEN 1")]
    [InlineData("{a:1 b:1}")]
    [InlineData("[1,2 3]")]
    [InlineData("true OR (x>1")]
    [InlineData("UPPER('abc'")]
    [InlineData("INDEXOF('abc''b')")]
    public void Execute_Constants(string expr)
    {
        Assert.Throws<LiteException> (() => BsonExpression.Create(expr));
    }
}
