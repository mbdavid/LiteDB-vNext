namespace LiteDB;

/// <summary>
/// Implement a data reader for a single value
/// </summary>
public class BsonScalarReader : IDataReader
{
    private readonly string _collection;
    private readonly BsonValue _value;
    private bool _init = false;

    /// <summary>
    /// Initialize data reader with created cursor
    /// </summary>
    internal BsonScalarReader(string collection, BsonValue value)
    {
        _collection = collection;
        _value = value;
    }

    /// <summary>
    /// Return current value
    /// </summary>
    public BsonValue Current => _value;

    /// <summary>
    /// Return collection name
    /// </summary>
    public string Collection => _collection;

    /// <summary>
    /// Move cursor to next result. Returns true if read was possible
    /// </summary>
    public ValueTask<bool> ReadAsync()
    {
        if (_init == false)
        {
            _init = true;

            return new ValueTask<bool>(true);
        }

        return new ValueTask<bool>(false);
    }

    public BsonValue this[string field] => _value.AsDocument[field];

    public void Dispose()
    {
    }
}