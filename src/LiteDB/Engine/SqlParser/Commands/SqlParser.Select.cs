namespace LiteDB.Engine;

/// <summary>
/// Internal class to parse and execute sql-like commands
/// </summary>
internal partial class SqlParser
{
    private Query ParseSelect()
    {
        _tokenizer.ReadToken().Expect("SELECT");

        throw new NotImplementedException();
    }
}
