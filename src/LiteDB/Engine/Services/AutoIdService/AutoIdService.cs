namespace LiteDB.Engine;

[AutoInterface(typeof(IDisposable))]
unsafe internal class AutoIdService : IAutoIdService
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
    public BsonValue SetDocumentID(byte colID, BsonDocument document, BsonAutoId autoId)
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
                var next = Interlocked.Increment(ref _sequences[colID].LastInt);

                document["_id"] = id = next;
            }
            else if (autoId == BsonAutoId.Int64)
            {
                var next = Interlocked.Increment(ref _sequences[colID].LastLong);

                document["_id"] = id = next;
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
    public void Initialize(byte colID, RowID tailIndexNodeID, IIndexService indexService)
    {
        var tail = indexService.GetNode(tailIndexNodeID);
        var last = indexService.GetNode(tail[0]->PrevID);

        if (last.Key->Type == BsonType.Int32)
        {
            _sequences[colID].LastInt = last.Key->ValueInt32;
        }
        else if (last.Key->Type == BsonType.Int32)
        {
            throw new NotImplementedException();
            //_sequences[colID].LastLong = last.Key->ValueInt64;
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
