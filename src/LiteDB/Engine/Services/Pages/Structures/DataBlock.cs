namespace LiteDB.Engine;

internal struct DataBlock
{
    /// <summary>
    /// Get fixed part of DataBlock (5 bytes)
    /// </summary>
    public const int DATA_BLOCK_FIXED_SIZE = 1 + // data block type (reserved)
                                             PageAddress.SIZE; // NextBlock

    public const int P_EXTEND = 0; // 00-00 [byte]
    public const int P_NEXT_BLOCK = 1; // 01-05 [pageAddress]
    public const int P_BUFFER = 6;     // 06-EOF [bytes[]]

    /// <summary>
    /// Data block DataBlockID on DataPage (not persisted)
    /// </summary>
    public readonly PageAddress DataBlockID;

    /// <summary>
    /// Indicate if this data block is first block (false) or extend block (true)
    /// </summary>
    public readonly bool Extend;

    /// <summary>
    /// If document need more than 1 block, use this link to next block
    /// </summary>
    public PageAddress NextBlockID;

    /// <summary>
    /// When datablock is first block, read DocumentLength from first 4 bytes. Otherwise, MaxValue (not persisted)
    /// </summary>
    public readonly int DocumentLength;

    /// <summary>
    /// Get how many bytes this data block contains only for document (not persisted)
    /// </summary>
    public readonly int DataLength;

    /// <summary>
    /// Read new DataBlock from filled page block
    /// </summary>
    public DataBlock(Span<byte> buffer, PageAddress dataBlockID)
    {
        this.DataBlockID = dataBlockID;

        this.Extend = buffer[P_EXTEND] != 0;
        this.NextBlockID = buffer[P_NEXT_BLOCK..].ReadPageAddress();

        this.DataLength = buffer.Length - DATA_BLOCK_FIXED_SIZE;
        this.DocumentLength = this.Extend ? int.MaxValue : buffer[P_BUFFER..].ReadVariantLength(out _);
    }

    /// <summary>
    /// Create new DataBlock and fill into buffer
    /// </summary>
    public DataBlock(Span<byte> buffer, PageAddress dataBlockID, bool extend)
    {
        this.DataBlockID = dataBlockID;
        this.Extend = extend;
        this.NextBlockID = PageAddress.Empty;

        buffer[P_EXTEND] = this.Extend ? (byte)1 : (byte)0;
        buffer[P_NEXT_BLOCK..].WritePageAddress(PageAddress.Empty);

        this.DataLength = buffer.Length - DATA_BLOCK_FIXED_SIZE;
        this.DocumentLength = int.MaxValue;
    }

    /// <summary>
    /// Update NextBlock pointer (update in buffer too)
    /// </summary>
    public void SetNextBlockID(Span<byte> buffer, PageAddress nextBlockID)
    {
        this.NextBlockID = nextBlockID;

        buffer[P_NEXT_BLOCK..].WritePageAddress(nextBlockID);
    }

    public override string ToString()
    {
        return Dump.Object(new { DataBlockID, Extend, NextBlockID });
    }
}
