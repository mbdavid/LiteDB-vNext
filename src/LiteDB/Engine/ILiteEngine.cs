namespace LiteDB.Engine;

/// <summary>
/// 
/// </summary>
public interface ILiteEngine : IDisposable
{
    EngineState State { get; }

    /// <summary>
    /// Retorna a exception que tirou o banco do ar (null caso ok e limpa no open())
    /// </summary>
    Exception FatalException { get; }

    #region Data Operations

    /// <summary>
    /// Retorna o _id
    /// </summary>
//    Task<BsonValue> InsertAsync(byte colID, ICollection<(int Index, BsonExpression Expr, bool Unique)> indexes, ICollection<BsonDocument> documents, int autoId);
//    Task<bool> UpdateAsync(byte colID, ICollection<BsonDocument> documents);
//    Task<bool> DeleteAsync(byte colID, ICollection<BsonValue> ids);

//    Task<object> BulkAsync(ICollection<object> operations);
    //Task<Guid> QueryAsync(PageAddress headerNode, object query, int buffer = 1024);
//    Task<Guid> FetchAsync(Guid cursorId);

    /// <summary>
    /// Implements a generic engine comunication using BsonDocument for input/output.
    /// This method can be used with plugins to call new features outside engine
    /// </summary>
    /// <param name="input">Data document as input parameter (see ...)</param>
    /// <returns>Data document result</returns>
//    Task<BsonDocument> ExecuteAsync(BsonDocument input);

    #endregion

    #region Exclusive Operations
    /// <summary>
    /// abre os arquivos, carrega header, faz recovery, e deixa o estado do banco pronto pra uso
    /// </summary>
    Task OpenAsync();

    Task ShutdownAsync(bool force);

//    Task<bool> CreateCollectionAsync(string name, object options);
//    Task<bool> DropCollectionAsync(string name);


//    Task<bool> CreateIndexAsync(string collection, string name, BsonExpression expression, bool unique);
//    Task<bool> DropIndexAsync(string collection, string name);


    /// <summary>
    /// 
    /// </summary>
    /// <param name="forced">Invalidate all pending cursors.</param>
    /// <returns></returns>
//    Task<int> CheckpointAsync(bool forced);
//    Task<int> RebuildAsync(object options);
//    Task<bool> SetPragmaAsync(string name, object value);

    #endregion
}
