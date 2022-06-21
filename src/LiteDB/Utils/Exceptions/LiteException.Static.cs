namespace LiteDB;

/// <summary>
/// All exceptions from LiteDB
/// </summary>
public partial class LiteException
{
    internal static LiteException ERR_FILE_NOT_FOUND(string filename) =>
        new(1, $"File '{filename}' not found.");

    internal static LiteException ERR_TOO_LARGE_VARIANT() =>
        new(2, $"Content too large and exceed 1Gb limit.");

    #region ERR_UNEXPECTED_TOKEN

    internal static LiteException ERR_UNEXPECTED_TOKEN(Token token, string expected = null)
    {
        var position = (token?.Position - (token?.Value?.Length ?? 0)) ?? 0;
        var str = token?.Type == TokenType.EOF ? "[EOF]" : token?.Value ?? "";
        var exp = expected == null ? "" : $" Expected `{expected}`.";

        return new (20, $"Unexpected token `{str}` in position {position}.{exp}")
        {
            Position = position
        };
    }

    internal static LiteException ERR_UNEXPECTED_TOKEN(string message, Token token)
    {
        var position = (token?.Position - (token?.Value?.Length ?? 0)) ?? 0;

        return new (20, message)
        {
            Position = position
        };
    }

    #endregion
}