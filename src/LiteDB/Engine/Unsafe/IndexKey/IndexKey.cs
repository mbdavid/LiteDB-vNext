namespace LiteDB.Engine;

[StructLayout(LayoutKind.Explicit, Size = 8, CharSet = CharSet.Ansi)]
unsafe internal partial struct IndexKey2
{
    [FieldOffset(0)] public BsonType Type;    // 1
    [FieldOffset(1)] public byte KeyLength;   // 1
    [FieldOffset(2)] public ushort Reserved;  // 2

    [FieldOffset(4)] public bool ValueBool;   // 1
    [FieldOffset(4)] public int ValueInt32;   // 4 

    /// <summary>
    /// Get how many bytes, in memory, this IndexKey are using
    /// </summary>
    public int IndexKeySize
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

    public override string ToString()
    {
        fixed(IndexKey2* indexKey = &this)
        {
            var value = ToBsonValue(indexKey);

            return Dump.Object(new { Type, KeyLength, IndexKeySize, Value = value.ToString() });
        }
    }
}
