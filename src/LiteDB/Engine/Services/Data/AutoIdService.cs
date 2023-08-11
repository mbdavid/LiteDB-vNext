namespace LiteDB.Engine;

[AutoInterface(typeof(IDisposable))]
internal class AutoIdService : IAutoIdService
{
    // dependency injection

    /// <summary>
    /// Sequences for all collections indexed by colID
    /// </summary>
    private readonly Sequence[] _sequences = new Sequence[byte.MaxValue];

    public AutoIdService()
    {
    }

    /// <summary>
    /// Set _id value according autoId and if not exists _id field. If exists, can update sequence if greater than last value
    /// </summary>
    public BsonValue SetID(byte colID, BsonDocument document, BsonAutoId autoId)
    {
        // if document as no _id, create a new
        if (!document.TryGetValue("_id", out var id))
        {
            if (autoId == BsonAutoId.ObjectId)
            {
                document["_id"] = id = ObjectId.NewObjectId();
            }
            else if (autoId == BsonAutoId.Guid)
            {
                document["_id"] = id = Guid.NewGuid();
            }
            else if (autoId == BsonAutoId.Int32)
            {
                document["_id"] = id = ++_sequences[colID].LastInt;
            }
            else if (autoId == BsonAutoId.Int64)
            {
                document["_id"] = id = ++_sequences[colID].LastLong;
            }
        }
        else
        {
            // test if new added value is larger than last sequence value
            if (autoId == BsonAutoId.Int32 && id.IsInt32)
            {
                var newId = id.AsInt32;

                if (newId > _sequences[colID].LastInt) _sequences[colID].LastInt = newId;
            }
            else if (autoId == BsonAutoId.Int64 && id.IsInt64)
            {
                var newId = id.AsInt64;

                if (newId > _sequences[colID].LastLong) _sequences[colID].LastLong = newId;
            }
        }

        return id;
    }

    /// <summary>
    /// Checks if a sequence already initialized
    /// </summary>
    public bool NeedInitialize(byte colID, BsonAutoId autoId) =>
        (autoId == BsonAutoId.Int32 || autoId == BsonAutoId.Int64) && _sequences[colID].LastInt != int.MaxValue;

    /// <summary>
    /// Initialize sequence based on last value on _id key.
    /// </summary>
    public async Task InitializeAsync(byte colID, PageAddress tailRowID, IIndexService indexService)
    {
        var tail = await indexService.GetNodeAsync(tailRowID, true); // must be writable because will be run over "insert"
        var last = await indexService.GetNodeAsync(tail.Node.Prev[0], true);

        if (last.Node.Key.IsInt32)
        {
            _sequences[colID].LastInt = last.Node.Key.AsInt32;
        }
        else if (last.Node.Key.IsInt64)
        {
            _sequences[colID].LastLong = last.Node.Key.AsInt64;
        }
        else
        {
            // initialize when last value is not an int/long
            _sequences[colID].LastInt = 0;
            _sequences[colID].LastLong = 0;
        }
    }

    public void Dispose()
    {
        // reset all sequences
        for(var i = 0; i < _sequences.Length; i++)
        {
            _sequences[i].Reset();
        }
    }
}

internal struct Sequence
{
    public int LastInt;
    public long LastLong;

    public Sequence()
    {
        this.Reset();
    }

    public void Reset()
    {
        this.LastInt = int.MaxValue;
        this.LastLong = long.MaxValue;
    }

    public override string ToString()
    {
        return $"{{ LastInt = {LastInt}, LastLong = {LastLong} }}";
    }
}