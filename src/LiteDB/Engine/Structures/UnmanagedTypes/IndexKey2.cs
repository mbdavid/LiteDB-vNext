namespace LiteDB.Engine;

[StructLayout(LayoutKind.Explicit, Size = 8, CharSet = CharSet.Ansi)]
unsafe internal struct IndexKey2
{
    [FieldOffset(0)] public BsonType Type;    // 1
    [FieldOffset(1)] public byte KeyLength;   // 1
    [FieldOffset(2)] public ushort Reserved;  // 2

    [FieldOffset(4)] public bool ValueBool;   // 1
    [FieldOffset(4)] public int ValueInt32;   // 4 

    /// <summary>
    /// Get how many bytes, in memory, this IndexKey are using
    /// </summary>
    public int IndexKeyLength
    {
        get
        {
            var header = sizeof(IndexKey2);
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
        var maxKeyLength = MAX_INDEX_KEY_SIZE - sizeof(IndexKey2);

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

        if (valueSize > maxKeyLength) throw ERR($"index value too excedded {maxKeyLength}");

        var header = sizeof(IndexKey2);
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

    public static int Compare(IndexKey2* left, IndexKey2* right, Collation collation)
    {
        // first, test if types are different
        if (left->Type != right->Type)
        {
            var leftIsNumber = left->Type == BsonType.Int32 || left->Type == BsonType.Int64 || left->Type == BsonType.Double || left->Type == BsonType.Decimal;
            var rightIsNumber = right->Type == BsonType.Int32 || right->Type == BsonType.Int64 || right->Type == BsonType.Double || right->Type == BsonType.Decimal;

            if (leftIsNumber && rightIsNumber)
            {
                return CompareNumbers(left, right);
            }
            // if not, order by sort type order
            else
            {
                var result = left->Type.CompareTo(right->Type);
                return result < 0 ? -1 : result > 0 ? +1 : 0;
            }
        }

        var leftValuePtr = (nint)left + sizeof(IndexKey2);
        var rightValuePtr = (nint)right + sizeof(IndexKey2);

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
            case BsonType.Decimal: return (*(decimal*)leftValuePtr).CompareTo(*(decimal*)rightValuePtr);

            case BsonType.String:
                var leftString = Encoding.UTF8.GetString((byte*)leftValuePtr, left->KeyLength);
                var rightString = Encoding.UTF8.GetString((byte*)rightValuePtr, right->KeyLength);

                return collation.Compare(leftString, rightString);

            case BsonType.Binary:
                var leftBinary = new Span<byte>((byte*)leftValuePtr, left->KeyLength);
                var rightBinary = new Span<byte>((byte*)leftValuePtr, left->KeyLength);                

                return leftBinary.SequenceCompareTo(rightBinary);
        }

        throw new NotSupportedException();
    }

    private static int CompareNumbers(IndexKey2* left, IndexKey2* right)
    {
        ENSURE(left->Type == BsonType.Int32 || left->Type == BsonType.Int64 || left->Type == BsonType.Double || left->Type == BsonType.Decimal);
        ENSURE(right->Type == BsonType.Int32 || right->Type == BsonType.Int64 || right->Type == BsonType.Double || right->Type == BsonType.Decimal);

        var leftValuePtr = (nint)left + sizeof(IndexNode);
        var rightValuePtr = (nint)right + sizeof(IndexNode);

        return (left->Type, right->Type) switch
        {
            (BsonType.Int32, BsonType.Int64) => left->ValueInt32.CompareTo(*(long*)rightValuePtr),
            (BsonType.Int32, BsonType.Double) => left->ValueInt32.CompareTo(*(double*)rightValuePtr),
            (BsonType.Int32, BsonType.Decimal) => left->ValueInt32.CompareTo(*(decimal*)rightValuePtr),

            (BsonType.Int64, BsonType.Int32) => (*(long*)leftValuePtr).CompareTo(left->ValueInt32),
            (BsonType.Int64, BsonType.Double) => (*(long*)leftValuePtr).CompareTo(*(double*)rightValuePtr),
            (BsonType.Int64, BsonType.Decimal) => (*(long*)leftValuePtr).CompareTo(*(decimal*)rightValuePtr),

            (BsonType.Double, BsonType.Int32) => (*(double*)leftValuePtr).CompareTo(left->ValueInt32),
            (BsonType.Double, BsonType.Int64) => (*(double*)leftValuePtr).CompareTo(*(long*)rightValuePtr),
            (BsonType.Double, BsonType.Decimal) => (*(double*)leftValuePtr).CompareTo(*(decimal*)rightValuePtr),

            (BsonType.Decimal, BsonType.Int32) => (*(decimal*)leftValuePtr).CompareTo(left->ValueInt32),
            (BsonType.Decimal, BsonType.Int64) => (*(decimal*)leftValuePtr).CompareTo(*(long*)rightValuePtr),
            (BsonType.Decimal, BsonType.Double) => (*(decimal*)leftValuePtr).CompareTo(*(double*)rightValuePtr),

            _ => throw new NotImplementedException()
        };
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
