namespace LiteDB.Engine;

/// <summary>
/// The DataPage thats stores object data.
/// </summary>
internal class DataPage : BlockPage
{
    /// <summary>
    /// Create new DataPage
    /// </summary>
    public DataPage(uint pageID, byte colID)
        : base(pageID, PageType.Data, colID)
    {
    }

    /// <summary>
    /// Load data page from buffer
    /// </summary>
    public DataPage(IMemoryOwner<byte> buffer)
        : base(buffer)
    {
        ENSURE(this.PageType == PageType.Data, "page type must be data page");
    }

    /// <summary>
    /// Get single DataBlock span buffer and outs DataBlock header info
    /// </summary>
    public Span<byte> GetDataBlock(byte index, bool readOnly, out DataBlock dataBlock)
    {
        var block = base.Get(index, readOnly, out var location);
        var rowID = new PageAddress(this.PageID, index);

        dataBlock = new DataBlock(block, rowID, location);

        return block[DataBlock.P_BUFFER..block.Length];
    }

    /// <summary>
    /// Insert a new datablock inside this page. Copy all content into a DataBlock
    /// </summary>
    public DataBlock InsertDataBlock(Span<byte> span, bool extend)
    {
        // get required bytes this update
        var bytesLength = (ushort)(span.Length + DataBlock.DATA_BLOCK_FIXED_SIZE);

        // get block from PageBlock
        var block = base.Insert(bytesLength, out var index, out var location);

        var rowID = new PageAddress(this.PageID, index);

        var dataBlock = new DataBlock(span, rowID, location, extend, PageAddress.Empty);

        // copy content from span source to block right position 
        span.CopyTo(block[DataBlock.P_BUFFER..block.Length]);

        return dataBlock;
    }

    /// <summary>
    /// Update data block content with new span buffer changes
    /// </summary>
    public void UpdateDataBlock(DataBlock dataBlock, Span<byte> span)
    {
        // get required bytes this update
        var bytesLength = (ushort)(span.Length + DataBlock.DATA_BLOCK_FIXED_SIZE);

        var block = base.Update(dataBlock.RowID.Index, bytesLength, out var location);

        // update dataBlock location (can be changed)
        dataBlock.Location = location;

        // copy content from span source to block right position 
        span.CopyTo(block[DataBlock.P_BUFFER..block.Length]);
    }

    /// <summary>
    /// Update DataBlock instance with NextBlock (when exceed page) and update on buffer
    /// </summary>
    public void UpdateNextBlock(DataBlock dataBlock, PageAddress nextBlock)
    {
        var span = _writeBuffer.Memory.Span;

        // get full page block from _writer using Location
        var block = span[dataBlock.Location.Position..(dataBlock.Location.Position + dataBlock.Location.Length)];

        dataBlock.UpdateNextBlock(nextBlock, block);
    }

    /// <summary>
    /// Delete single data block inside this page
    /// </summary>
    public void DeleteBlock(byte index)
    {
        base.Delete(index);
    }
}
