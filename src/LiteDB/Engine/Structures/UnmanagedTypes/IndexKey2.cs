namespace LiteDB.Engine;

[StructLayout(LayoutKind.Explicit, Size = 8, CharSet = CharSet.Ansi)]
unsafe internal struct IndexKey2
{
    [FieldOffset(0)] public BsonType Type;    // 1
    [FieldOffset(1)] public byte KeyLength;   // 1
    [FieldOffset(2)] public ushort Reserved;  // 2

    [FieldOffset(4)] public bool ValueBool;   // 1
    [FieldOffset(4)] public int ValueInt32;   // 4 

    private const int INDEX_KEY_HEADER_SIZE = 8; // long

    /// <summary>
    /// Get how many bytes, in memory, this IndexKey are using
    /// </summary>
    public int IndexKeyLength
    {
        get
        {
            var header = sizeof(long);
            var valueSize = this.Type switch
            {
                BsonType.Boolean => 0, // 0 (1 but use header hi-space)
                BsonType.Int32 => 0,   // 0 (4 but use header hi-space)
                _ => this.KeyLength
            };
            var padding = valueSize % 8 > 0 ? 8 - (valueSize % 8) : 0;

            return header + valueSize + padding;
        }
    }

    /// <summary>
    /// Get how many bytes IndexKey structure will need to represent this BsonValue (should be padded)
    /// </summary>
    public static int GetIndexKeyLength(BsonValue value, out int valueSize)
    {
        const int MAX_KEY_LENGTH = MAX_INDEX_KEY_SIZE - INDEX_KEY_HEADER_SIZE; // header 8 bytes

        valueSize = value.Type switch
        {
            BsonType.MaxValue => 0,
            BsonType.MinValue => 0,
            BsonType.Null => 0,
            BsonType.Boolean => 0, // use indexKey header hi 4-bytes
            BsonType.Int32 => 0, // use indexKey header hi 4-bytes
            BsonType.Int64 => sizeof(long), // 8
            BsonType.Double => sizeof(double), // 8
            BsonType.DateTime => sizeof(DateTime), // 8
//            BsonType.ObjectId => sizeof(ObjectId), // 12
            BsonType.Decimal => sizeof(decimal), // 16
            BsonType.Guid => sizeof(Guid), // 16
            BsonType.String => Encoding.UTF8.GetByteCount(value.AsString),
            BsonType.Binary => value.AsBinary.Length,
            _ => throw ERR($"This object type `{value.Type}` are not supported as an index key")
        };

        if (valueSize > MAX_KEY_LENGTH) throw ERR($"index value too excedded {MAX_KEY_LENGTH}");

        var header = sizeof(long);
        var padding = valueSize % 8 > 0 ? 8 - (valueSize % 8) : 0;
        var result = header + valueSize + padding;

        return result;
        //Console.WriteLine(Dump.Object(new { Type = value.Type, header, valueSize, padding, result }));
    }

    /// <summary>
    /// Allocate, in unmanaged memory, a new IndexKey instance based on BsonValue. Must call FreeIndexKey after use
    /// </summary>
    public static IndexKey2* AllocNewIndexKey(BsonValue value)
    {
        // get full indexKey allocation memory size
        var indexKeyLength = GetIndexKeyLength(value, out var valueSize);

        var indexKey = (IndexKey2*)Marshal.AllocHGlobal(indexKeyLength);

        // set fixed values
        indexKey->Type = value.Type;
        indexKey->Reserved = 0;
        indexKey->KeyLength = (byte)valueSize; // it's wrong for in bool/int (fix below)

        var ptr = (nint)indexKey + sizeof(long); // get content out of IndexKey structure

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
                //*(ObjectId*)ptr = value.AsObjectId; // convert pointer into ObjectId* to set value
                throw new NotImplementedException();
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

    public static int Compare(IndexKey2* left, IndexKey2* right, Collation collection)
    {
        // first, test if types are different
        if (left->Type != right->Type)
        {
            // if both values are number, convert them to Decimal (128 bits) to compare
            // it's the slowest way, but more secure
            if (left.IsNumber && right.IsNumber)
            {
                return Convert.ToDecimal(this.RawValue).CompareTo(Convert.ToDecimal(other.RawValue));
            }
            // if not, order by sort type order
            else
            {
                var result = left->Type.CompareTo(right->Type);
                return result < 0 ? -1 : result > 0 ? +1 : 0;
            }
        }

        var leftValuePtr = (nint)left + sizeof(long);
        var rightValuePtr = (nint)right + sizeof(long);

        switch (left->Type)
        {
            case BsonType.MinValue:
            case BsonType.MaxValue:
            case BsonType.Null: 
                return 0;
            case BsonType.Boolean: return left->ValueBool.CompareTo(right->ValueBool);
            case BsonType.Int32: return left->ValueInt32.CompareTo(right->ValueInt32);

            case BsonType.Int64: return (*(long*)leftValuePtr).CompareTo(*(long*)rightValuePtr);
            case BsonType.Double: return (*(double*)leftValuePtr).CompareTo(*(double*)rightValuePtr);
            case BsonType.DateTime: return (*(DateTime*)leftValuePtr).CompareTo(*(DateTime*)rightValuePtr);

//            case BsonType.ObjectId: return (*(ObjectId*)leftValuePtr).CompareTo(*(ObjectId*)rightValuePtr);
            case BsonType.Guid: return (*(Guid*)leftValuePtr).CompareTo(*(Guid*)rightValuePtr);
            case BsonType.Decimal: return (*(Decimal*)leftValuePtr).CompareTo(*(Decimal*)rightValuePtr);

            case BsonType.String:
                var leftString = Encoding.UTF8.GetString((byte*)leftValuePtr, left->KeyLength);
                var rightString = Encoding.UTF8.GetString((byte*)rightValuePtr, left->KeyLength);

                return collection.Compare();

        }



        throw new NotSupportedException();
    }

    /// <summary>
    /// Convert IndexKey pointer into a managed BsonValue
    /// </summary>
    public static BsonValue ToBsonValue(IndexKey2* indexKey)
    {
        var ptr = (nint)indexKey + sizeof(long);

        return indexKey->Type switch
        {
            BsonType.MinValue => BsonValue.MinValue,
            BsonType.MaxValue => BsonValue.MaxValue,
            BsonType.Null => BsonValue.Null,

            BsonType.Boolean => indexKey->ValueBool,
            BsonType.Int32 => indexKey->ValueInt32,

            BsonType.Int64 => *(long*)ptr,
            BsonType.Double => *(double*)ptr,
            BsonType.DateTime => *(DateTime*)ptr,

            //BsonType.ObjectId => *(ObjectId*)ptr,
            BsonType.Guid => *(Guid*)ptr,
            BsonType.Decimal => *(decimal*)ptr,

            BsonType.String => Encoding.UTF8.GetString((byte*)ptr, indexKey->KeyLength),
            BsonType.Binary => new Span<byte>((byte*)ptr, indexKey->KeyLength).ToArray(),

            _ => throw new NotSupportedException()
        };

    }

    public override string ToString()
    {
        fixed(IndexKey2* indexKey = &this)
        {
            var value = ToBsonValue(indexKey);

            return Dump.Object(new { Type, KeyLength, IndexKeyLength, Value = value.ToString() });
        }
    }
}
