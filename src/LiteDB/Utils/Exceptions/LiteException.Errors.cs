namespace LiteDB;

/// <summary>
/// All exceptions from LiteDB
/// </summary>
public partial class LiteException
{
    internal static LiteException ERR(string message) =>
        new(0, message);

    internal static LiteException ERR_FILE_NOT_FOUND(string filename) =>
        new(1, $"File '{filename}' not found.");

    internal static LiteException ERR_TOO_LARGE_VARIANT() =>
        new(2, $"Content too large and exceed 1Gb limit.");

    internal static LiteException ERR_TIMEOUT(TimeSpan timeout) =>
        new(3, $"Timeout exceeded. Limit: {timeout.TotalSeconds:0}");

    #region ERR_UNEXPECTED_TOKEN

    internal static LiteException ERR_UNEXPECTED_TOKEN(Token token, string? expected = null)
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

    #region CRITITAL ERRORS (stop engine)

    internal static LiteException ERR_INVALID_DATABASE() =>
        new(900, $"File is not a valid LiteDB database format or contains a invalid password.");

    internal static LiteException ERR_INVALID_FILE_VERSION() =>
        new(900, $"Invalid database file version.");

    internal static LiteException ERR_INVALID_FREE_SPACE_PAGE(uint pageID, int freeBytes, int length) =>
        new(901, $"An operation that would corrupt page {pageID} was prevented. The operation required {length} free bytes, but the page had only {freeBytes} available.");

    internal static LiteException ERR_ENSURE(string? message) =>
        new(902, $"ENSURE: {message}.");

    internal static LiteException ERR_DATAFILE_NOT_ENCRYPTED() =>
        new(903, $"This datafile are not encrypted and shoutn't provide password");

    internal static LiteException ERR_DISK_WRITE_FAILURE(Exception ex) =>
        new(904, $"Disk fail in write operation: {ex.Message}. See inner exception for details", ex);

    #endregion
}