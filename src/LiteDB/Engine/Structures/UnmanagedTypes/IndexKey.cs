namespace LiteDB.Engine;

[StructLayout(LayoutKind.Explicit, CharSet = CharSet.Ansi)]
unsafe internal struct IndexKey : IComparable<IndexKey>, IEquatable<IndexKey>
{
    [FieldOffset(0)] public BsonType DataType; // 1
    [FieldOffset(1)] public byte Length;       // 1

    [FieldOffset(2)] public int ValueInt32;
    [FieldOffset(2)] public long ValueInt64;

    public static IndexKey MinValue = new() { DataType = BsonType.MinValue, Length = 0 };
    public static IndexKey MaxValue = new() { DataType = BsonType.MinValue, Length = 0 };

    public bool IsMinValue => this.DataType == BsonType.MinValue;
    public bool IsMaxValue => this.DataType == BsonType.MaxValue;
    public bool IsInt32 => this.DataType == BsonType.Int32;
    public bool IsInt64 => this.DataType == BsonType.Int64;

    public IndexKey()
    {
    }

    public IndexKey(BsonValue value)
    {
        this.DataType = value.Type;

        //var xx = Marshal.PtrToStringAnsi((nint)myStructPtr + 2, 4);

        switch (value.Type)
        {
            case BsonType.Int32: this.ValueInt32 = value.AsInt32; break;
            case BsonType.Int64: this.ValueInt64 = value.AsInt64; break;

            default: throw ERR($"BsonValue not supported for index key: {value}");
        }
    }

    public static implicit operator BsonValue(IndexKey value) => throw new NotImplementedException();

    public static implicit operator IndexKey(BsonValue value) => new IndexKey(value);

    public void CopyFrom(IndexKey indexKey)
    {
        this.DataType = indexKey.DataType;
        this.Length = indexKey.Length;

        switch(indexKey.DataType)
        {
            case BsonType.Int32: this.ValueInt32 = indexKey.ValueInt32; break;
            case BsonType.Int64: this.ValueInt64 = indexKey.ValueInt64; break;

            default: throw new NotSupportedException();
        }


    }

    public static int Compare(IndexKey left, IndexKey right, Collation collation)
    {

        if (left.DataType == BsonType.Int32 &&  right.DataType == BsonType.Int32)
        {
            return left.ValueInt32 - right.ValueInt32;
        }


        return 0;
    }

    public static int Compare(IndexKey* left, IndexKey* right, Collation collation)
    {
        if (left->DataType == BsonType.Int32 && right->DataType == BsonType.Int32)
        {
            return left->ValueInt32 - right->ValueInt32;
        }


        return 0;
    }

    public static int Compare(IndexKey left, IndexKey* right, Collation collation)
    {
        if (left.DataType == BsonType.Int32 && right->DataType == BsonType.Int32)
        {
            return left.ValueInt32 - right->ValueInt32;
        }


        return 0;
    }

    public static int Compare(IndexKey* left, IndexKey right, Collation collation)
    {
        if (left->DataType == BsonType.Int32 && right.DataType == BsonType.Int32)
        {
            return left->ValueInt32 - right.ValueInt32;
        }


        return 0;
    }

    public bool Equals(IndexKey other) => Compare(this, other, Collation.Default) == 0;

    public int CompareTo(IndexKey other) => Compare(this, other, Collation.Default);
    public int CompareTo(IndexKey other, Collation collation) => Compare(this, other, collation);
}
