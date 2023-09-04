namespace LiteDB.Engine;

[StructLayout(LayoutKind.Explicit, CharSet = CharSet.Ansi)]
unsafe internal struct IndexKey : IComparable<IndexKey>, IEquatable<IndexKey>
{
    [FieldOffset(0)] public BsonType Type;   // 1
    [FieldOffset(1)] public byte Length;     // 1

    [FieldOffset(2)] public ushort Reserved; // 2

    [FieldOffset(4)] public int ValueInt32;    // .. always same position
    [FieldOffset(4)] public uint ValueUInt32;  // .. always same position

    public static IndexKey MinValue = new() { Type = BsonType.MinValue, Length = 0 };
    public static IndexKey MaxValue = new() { Type = BsonType.MinValue, Length = 0 };

    public bool IsMinValue => this.Type == BsonType.MinValue;
    public bool IsMaxValue => this.Type == BsonType.MaxValue;
    public bool IsInt32 => this.Type == BsonType.Int32;
    public bool IsInt64 => this.Type == BsonType.Int64;
    public bool IsNull => this.Type == BsonType.Null;

    public IndexKey()
    {
    }

    public IndexKey(BsonValue value)
    {
        this.Type = value.Type;

        //var xx = Marshal.PtrToStringAnsi((nint)myStructPtr + 2, 4);
        this.ValueInt32 = 0;
        this.Length = 0;

        switch (value.Type)
        {
            case BsonType.Int32: this.ValueInt32 = value.AsInt32; break;
            //case BsonType.Int64: this.ValueInt64 = value.AsInt64; break;

            default: throw ERR($"BsonValue not supported for index key: {value}");
        }
    }

    public static implicit operator BsonValue(IndexKey value) => throw new NotImplementedException();

    public static implicit operator IndexKey(BsonValue value) => new IndexKey(value);

    public static void CopyValues(IndexKey from, IndexKey* to)
    {
        to->Type = from.Type;
        to->Length = 8; // from.Length;

        switch(from.Type)
        {
            case BsonType.Int32: to->ValueInt32 = from.ValueInt32; break;
            //case BsonType.Int64: to->ValueInt64 = from.ValueInt64; break;

            //default: throw new NotImplementedException();
        }


    }

    public static int Compare(IndexKey left, IndexKey right, Collation collation)
    {

        if (left.Type == BsonType.Int32 &&  right.Type == BsonType.Int32)
        {
            return left.ValueInt32 - right.ValueInt32;
        }


        return 0;
    }

    public static int Compare(IndexKey* left, IndexKey* right, Collation collation)
    {
        if (left->Type == BsonType.Int32 && right->Type == BsonType.Int32)
        {
            return left->ValueInt32 - right->ValueInt32;
        }


        return 0;
    }

    public static int Compare(IndexKey left, IndexKey* right, Collation collation)
    {
        if (left.Type == BsonType.Int32 && right->Type == BsonType.Int32)
        {
            return left.ValueInt32 - right->ValueInt32;
        }


        return 0;
    }

    public static int Compare(IndexKey* left, IndexKey right, Collation collation)
    {
        if (left->Type == BsonType.Int32 && right.Type == BsonType.Int32)
        {
            return left->ValueInt32 - right.ValueInt32;
        }


        return 0;
    }

    public bool Equals(IndexKey other) => Compare(this, other, Collation.Default) == 0;

    public int CompareTo(IndexKey other) => Compare(this, other, Collation.Default);
    public int CompareTo(IndexKey other, Collation collation) => Compare(this, other, collation);

    public override string ToString()
    {
        return Dump.Object(new { Type, Length, ValueInt32 });
    }
}
