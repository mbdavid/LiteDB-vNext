namespace LiteDB.Engine;

/// <summary>
/// Internal class to parse and execute sql-like commands
/// </summary>
internal partial class SqlParser
{
    private Tokenizer _tokenizer;
    private Collation _collation;

    public IScalarStatement ParseStatement()
    {
        var ahead = _tokenizer.LookAhead().Expect(TokenType.Word);

        switch (ahead.Value.ToUpper())
        {
            //case "SELECT":
            //case "EXPLAIN":
            //    return this.ParseSelect();
            case "INSERT": return this.ParseInsert();
            //case "DELETE": return this.ParseDelete();
            //case "UPDATE": return this.ParseUpdate();
            //case "DROP": return this.ParseDrop();
            //case "RENAME": return this.ParseRename();
            //case "CREATE": return this.ParseCreate();
            //
            //case "CHECKPOINT": return this.ParseCheckpoint();
            //case "REBUILD": return this.ParseRebuild();
            //
            //case "BEGIN": return this.ParseBegin();
            //case "ROLLBACK": return this.ParseRollback();
            //case "COMMIT": return this.ParseCommit();
            //
            //case "PRAGMA": return this.ParsePragma();

            default: throw ERR_UNEXPECTED_TOKEN(ahead);
        }

    }
}