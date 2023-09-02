namespace LiteDB.Engine;

unsafe internal struct IndexKey 
{
    public BsonType DataType; // 1
    public byte Length;       // 1

    public int ValueInt32;
    public long ValueInt64;

    public IndexKey()
    {
    }

    public IndexKey(BsonValue value)
    {
        this.DataType = value.Type;

        switch(value.Type)
        {
            case BsonType.Int32: this.ValueInt32 = value.AsInt32; break;
            case BsonType.Int64: this.ValueInt64 = value.AsInt64; break;

            default: throw ERR($"BsonValue not supported for index key: {value}");
        }
    }

    public static int Compare(IndexKey left, IndexKey right)
    {
        if (left.DataType == BsonType.Int32 &&  right.DataType == BsonType.Int32)
        {
            return left.ValueInt32 - right.ValueInt32;
        }


        return 0;
    }

    public static int Compare(IndexKey* left, IndexKey* right)
    {
        if (left->DataType == BsonType.Int32 && right->DataType == BsonType.Int32)
        {
            return left->ValueInt32 - right->ValueInt32;
        }


        return 0;
    }
}
