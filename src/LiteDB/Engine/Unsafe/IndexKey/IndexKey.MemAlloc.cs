namespace LiteDB.Engine;

unsafe internal partial struct IndexKey2
{
    /// <summary>
    /// Allocate, in unmanaged memory, a new IndexKey instance based on BsonValue. Must call FreeIndexKey after use
    /// </summary>
    public static IndexKey2* AllocNewIndexKey(BsonValue value)
    {
        // get full indexKey allocation memory size
        var indexKeyLength = GetSize(value, out var valueSize);

        var indexKey = (IndexKey2*)Marshal.AllocHGlobal(indexKeyLength);

        // set fixed values
        indexKey->Type = value.Type;
        indexKey->Reserved = 0;
        indexKey->KeyLength = (byte)valueSize; // it's wrong for in bool/int (fix below)

        var ptr = (nint)indexKey + sizeof(IndexKey2); // get content out of IndexKey structure

        // copy value according with BsonType (use ptr as starting point - extect Bool/Int32)
        switch (value.Type)
        {
            case BsonType.Boolean: // 1
                indexKey->KeyLength = sizeof(bool);
                indexKey->ValueBool = value.AsBoolean;
                break;
            case BsonType.Int32: // 4
                indexKey->KeyLength = sizeof(int);
                indexKey->ValueInt32 = value.AsInt32;
                break;
            case BsonType.Int64: // 8
                *(long*)ptr = value.AsInt64; // convert pointer into long* to set value
                break;
            case BsonType.Double: // 8
                *(double*)ptr = value.AsDouble; // convert pointer into double* to set value
                break;
            case BsonType.DateTime: // 8
                *(DateTime*)ptr = value.AsDateTime; // convert pointer into DateTime* to set value
                break;
            case BsonType.ObjectId: // 12
                *(ObjectId*)ptr = value.AsObjectId; // convert pointer into ObjectId* to set value
                break;
            case BsonType.Guid: // 16
                *(Guid*)ptr = value.AsGuid; // convert pointer into Guid* to set value
                break;
            case BsonType.Decimal: // 16
                *(decimal*)ptr = value.AsDecimal; // convert pointer into decimal* to set value
                break;

            case BsonType.String:
                var strSpan = new Span<byte>((byte*)ptr, indexKey->KeyLength);
                Encoding.UTF8.GetBytes(value.AsString, strSpan); // copy string content into indexKey value buffer
                break;

            case BsonType.Binary:
                var binSpan = new Span<byte>((byte*)ptr, indexKey->KeyLength);
                value.AsBinary.CopyTo(binSpan);
                break;
        }

        return indexKey;
    }

    /// <summary>
    /// Free unmanaged memory used to IndexKey
    /// </summary>
    public static void FreeIndexKey(IndexKey2* indexKey)
    {
        Marshal.FreeHGlobal((nint)indexKey);
    }
}
