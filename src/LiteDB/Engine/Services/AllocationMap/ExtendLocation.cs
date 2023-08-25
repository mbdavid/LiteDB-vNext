namespace LiteDB.Engine;

internal struct ExtendLocation
{
    public readonly int AllocationMapID;
    public readonly int ExtendIndex;

    public bool IsEmpty => this.AllocationMapID == -1 && this.ExtendIndex == -1;

    public int ExtendID => this.IsEmpty ? -1 : (this.AllocationMapID * AM_EXTEND_COUNT) + this.ExtendIndex;

    public int FirstPageID => this.AllocationMapID * AM_PAGE_STEP + this.ExtendIndex * AM_EXTEND_SIZE + 1;

    public ExtendLocation()
    {
        this.AllocationMapID = 0;
        this.ExtendIndex = 0;
    }

    public ExtendLocation(int allocationMapID, int extendIndex)
    {
        this.AllocationMapID = allocationMapID;
        this.ExtendIndex = extendIndex;

        ENSURE(extendIndex < AM_EXTEND_COUNT, new { self = this });
    }

    public ExtendLocation(int extendID)
    {
        this.AllocationMapID = extendID / AM_EXTEND_COUNT;
        this.ExtendIndex = extendID % AM_EXTEND_COUNT;
    }


    public override string ToString()
    {
        return $"{{ AMP = {AllocationMapID}, ExtIndex = {ExtendIndex}, ExtID = {ExtendID} }}";
    }
}
