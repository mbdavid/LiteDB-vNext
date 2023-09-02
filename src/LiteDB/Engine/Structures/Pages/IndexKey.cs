namespace LiteDB.Engine;

[StructLayout(LayoutKind.Explicit, CharSet = CharSet.Ansi)]
unsafe internal struct IndexKey 
{
    [FieldOffset(0)] public BsonType DataType; // 1
    [FieldOffset(1)] public byte Length;       // 1

    [FieldOffset(2)] public int ValueInt32;
    [FieldOffset(2)] public long ValueInt64;

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
}
