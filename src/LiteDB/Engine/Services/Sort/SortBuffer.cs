namespace LiteDB.Engine;

internal class SortBuffer
{
    public int PositionID { get; set; }

    public Memory<byte> Buffer = new byte[CONTAINER_SORT_SIZE];
}
