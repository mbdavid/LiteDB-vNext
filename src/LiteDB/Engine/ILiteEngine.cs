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
    Task<BsonValue> InsertAsync(string collection, ICollection<BsonDocument> documents, int autoId);
    Task<bool> UpdateAsync(string collection, ICollection<BsonDocument> documents);
    Task<bool> DeleteAsync(string collection, ICollection<BsonValue> ids);

    Task<object> BulkAsync(IList<object> operations);
    Task<Guid> QueryAsync(string collection, object query, int buffer = 1024);
    Task<Guid> FetchAsync(Guid cursorId);

    #endregion

    #region Exclusive Operations
    /// <summary>
    /// abre os arquivos, carrega header, faz recovery, e deixa o estado do banco pronto pra uso
    /// </summary>
    Task OpenAsync();

    Task ShutdownAsync(bool force);

    Task<bool> CreateCollectionAsync(string name, object options);
    Task<bool> DropCollectionAsync(string name);


    Task<bool> CreateIndexAsync(string collection, string name, BsonExpression expression, bool unique);
    Task<bool> DropIndexAsync(string collection, string name);


    /// <summary>
    /// 
    /// </summary>
    /// <param name="forced">Invalidate all pending cursors.</param>
    /// <returns></returns>
    Task<int> CheckpointAsync(bool forced);
    Task<int> RebuildAsync(object options);
    Task<bool> SetPragmaAsync(string name, object value);

    #endregion
}
