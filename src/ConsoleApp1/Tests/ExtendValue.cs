using LiteDB.Engine;

uint extendValue = 0b00000001_100_100_100_100_100_100_100_010;
byte colID = 1;
var pageType = PageType.Data;
var length = 3500;


var result = AllocationMapPage.HasFreeSpaceInExtend(extendValue, colID, pageType, length);

Console.WriteLine(Convert.ToString(extendValue, 2).PadLeft(32, '0'));

Console.Write(result);