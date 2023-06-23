namespace LiteDB.Engine;

internal enum PageTypeSlot : byte 
{ 
    Empty = 0, 
    Index = 1, 
    Data = 2,
    Reserved = 3
}