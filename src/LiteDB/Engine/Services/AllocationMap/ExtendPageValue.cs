namespace LiteDB.Engine;

internal enum ExtendPageValue : byte
{
    Empty = 0, // 00
    Data = 1,  // 01
    Index = 2, // 10
    Full = 3,  // 11
}
