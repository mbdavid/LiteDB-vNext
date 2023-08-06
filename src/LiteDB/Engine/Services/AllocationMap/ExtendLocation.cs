namespace LiteDB.Engine;

internal readonly struct ExtendLocation
{
    public static ExtendLocation Empty = new(-1, -1);
    public static ExtendLocation First = new(0, 0);

    public readonly int AllocationMapID;
    public readonly int ExtendIndex;

    public readonly int ExtendID => this.IsEmpty ? -1 : (this.AllocationMapID * AM_EXTEND_COUNT) + this.ExtendIndex;

    public ExtendLocation(int extendID)
    {
        this.AllocationMapID = extendID / AM_EXTEND_COUNT;
        this.ExtendIndex = extendID % AM_EXTEND_COUNT;
    }

    public ExtendLocation(int allocationMapID, int extendIndex)
    {
        this.AllocationMapID = allocationMapID;
        this.ExtendIndex = extendIndex;
    }

    public readonly bool IsEmpty => 
        this.AllocationMapID == Empty.AllocationMapID && 
        this.ExtendIndex == Empty.ExtendIndex;

    public readonly ExtendLocation Next()
    {
        var allocationMapID = this.AllocationMapID;
        var extendIndex = this.ExtendIndex + 1;

        if (extendIndex >= AM_EXTEND_COUNT)
        {
            extendIndex = 0;
            allocationMapID++;
        }

        return new ExtendLocation(allocationMapID, extendIndex);
    }

    public override readonly string ToString()
    {
        return $"AMP: {this.AllocationMapID}, ExtIndex: {this.ExtendIndex}, ExtendID: {this.ExtendID}";
    }
}
