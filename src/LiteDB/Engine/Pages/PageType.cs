﻿namespace LiteDB.Engine;

internal enum PageType : byte 
{ 
    Empty = 0, 
    Header = 1, 
    FreeSpace = 2, 
    Index = 3, 
    Data = 4 
}