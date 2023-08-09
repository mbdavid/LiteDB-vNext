namespace LiteDB.Engine;

internal struct ExtendLocation
{
    public static ExtendLocation Empty = new(-1, -1);
    public static ExtendLocation First = new(0, 0);

    public readonly int AllocationMapID;
    public readonly int ExtendIndex;

    public int ExtendID => this.IsEmpty ? -1 : (this.AllocationMapID * AM_EXTEND_COUNT) + this.ExtendIndex;

    public int FirstPageID => this.AllocationMapID * AM_PAGE_STEP + this.ExtendIndex * AM_EXTEND_SIZE + 1;

    public ExtendLocation(int extendID)
    {
        this.AllocationMapID = extendID / AM_EXTEND_COUNT;
        this.ExtendIndex = extendID % AM_EXTEND_COUNT;
    }

    public ExtendLocation(int allocationMapID, int extendIndex)
    {
        this.AllocationMapID = allocationMapID;
        this.ExtendIndex = extendIndex;

        ENSURE(() => extendIndex < AM_EXTEND_COUNT);
    }

    public bool IsEmpty => 
        this.AllocationMapID == Empty.AllocationMapID && 
        this.ExtendIndex == Empty.ExtendIndex;

    public ExtendLocation Next()
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

    public override string ToString()
    {
        return $"AMP: {this.AllocationMapID}, ExtIndex: {this.ExtendIndex}, ExtendID: {this.ExtendID}";
    }
}
