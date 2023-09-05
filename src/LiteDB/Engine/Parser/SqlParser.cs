﻿namespace LiteDB.Engine;

/// <summary>
/// Internal class to parse and execute sql-like commands
/// </summary>
public partial class SqlParser
{
    public SqlParser(object engine)
    {
    }

    public Func<BsonDocument, object> Compile(ReadOnlySpan<char> input)
    {
        throw new NotImplementedException();
    }

    public object Execute(ReadOnlySpan<char> input, BsonDocument parameters)
    {
        throw new NotImplementedException();
        // possivel cache de input?
    }

    //private readonly I__LiteEngine _engine;
    //private readonly Tokenizer _tokenizer;
    //private readonly BsonDocument _parameters;
    //private readonly Lazy<Collation> _collation;

    //public SqlParser(I__LiteEngine engine, Tokenizer tokenizer, BsonDocument parameters)
    //{
    //    _engine = engine;
    //    _tokenizer = tokenizer;
    //    _parameters = parameters ?? new BsonDocument();
    //    _collation = new Lazy<Collation>(() => new Collation(_engine.Pragma(Pragmas.COLLATION)));
    //}

    //public IBsonDataReader Execute()
    //{
    //    var ahead = _tokenizer.LookAhead().Expect(TokenType.Word);

    //    LOG($"executing `{ahead.Value.ToUpper()}`", "SQL");

    //    switch (ahead.Value.ToUpper())
    //    {
    //        case "SELECT": 
    //        case "EXPLAIN":
    //            return this.ParseSelect();
    //        case "INSERT": return this.ParseInsert();
    //        case "DELETE": return this.ParseDelete();
    //        case "UPDATE": return this.ParseUpdate();
    //        case "DROP": return this.ParseDrop();
    //        case "RENAME": return this.ParseRename();
    //        case "CREATE": return this.ParseCreate();

    //        case "CHECKPOINT": return this.ParseCheckpoint();
    //        case "REBUILD": return this.ParseRebuild();

    //        case "BEGIN": return this.ParseBegin();
    //        case "ROLLBACK": return this.ParseRollback();
    //        case "COMMIT": return this.ParseCommit();

    //        case "PRAGMA": return this.ParsePragma();

    //        default:  throw LiteException.UnexpectedToken(ahead);
    //    }
    //}
}