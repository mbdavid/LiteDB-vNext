namespace LiteDB;

/// <summary>
/// Criticals exception - should stop database
/// </summary>
public partial class LiteException
{
    internal static LiteException ERR_INVALID_DATABASE() =>
        new(900, $"File is not a valid LiteDB database format or contains a invalid password.");

    internal static LiteException ERR_INVALID_FREE_SPACE_PAGE(uint pageID, int freeBytes, int length) =>
        new(901, $"An operation that would corrupt page {pageID} was prevented. The operation required {length} free bytes, but the page had only {freeBytes} available.");

    internal static LiteException ERR_ENSURE(string message) =>
        new(902, $"ENSURE: {message}.");
}