namespace LiteDB.Engine;

/// <summary>
/// BlockPage are page based in data blocks (used for DataPage and IndexPage only)
/// </summary>
internal class BlockPage : BasePage
{
    private readonly BlockPageHeader _header;

    private BlockPageHeader _headerWrite = null;

    #region Buffer Field Positions

    public const int P_COL_ID = 5; // 5-5 [byte]
    public const int P_TRANSACTION_ID = 6; // 6-10 [uint]
    public const int P_IS_CONFIRMED = 11; // 11-11 [byte]

    #endregion

    #region Properties

    /// <summary>
    /// Get/Set collection ID index
    /// </summary>
    public byte ColID { get; }

    /// <summary>
    /// Represent transaction ID that was stored [4 bytes]
    /// </summary>
    public uint TransactionID { get; set; }

    /// <summary>
    /// Used in WAL, define this page is last transaction page and are confirmed on disk [1 byte]
    /// </summary>
    public bool IsConfirmed { get; set; }

    #endregion

    /// <summary>
    /// Create a new BlockPage
    /// </summary>
    public BlockPage(uint pageID, PageType pageType, byte colID)
        : base(pageID, pageType)
    {
        // ColID never change
        this.ColID = colID;

        // write unchanged data
        var span = _writeBuffer.Memory.Span;

        span[P_COL_ID] = this.ColID;

        // initialize an empty header
        _header = new BlockPageHeader();
    }

    /// <summary>
    /// Load BlockPage from buffer memory
    /// </summary>
    public BlockPage(IMemoryOwner<byte> buffer)
        : base(buffer)
    {
        var span = buffer.Memory.Span;

        // initialize variables with buffer data
        this.ColID = span[P_COL_ID];
        this.TransactionID = span[P_TRANSACTION_ID..].ReadUInt32();
        this.IsConfirmed = span[P_IS_CONFIRMED] != 0;

        // load header page with buffer data
        _header = new BlockPageHeader(span);
    }

    /// <summary>
    /// Inicialize write header with a copy of read header
    /// </summary>
    protected override void InitializeWrite()
    {
        base.InitializeWrite();

        _headerWrite = new BlockPageHeader(_header);
    }

    /// <summary>
    /// Get updated write buffer
    /// </summary>
    public override Memory<byte> GetBufferWrite()
    {
        var buffer = base.GetBufferWrite();
        var span = buffer.Span;

        // update header props
        span[P_TRANSACTION_ID..].WriteUInt32(this.TransactionID);
        span[P_IS_CONFIRMED] = this.IsConfirmed ? (byte)1 : (byte)0;

        // update header instance
        _headerWrite.Update(span);

        return buffer;
    }

    #region Blocks Operations

    /// <summary>
    /// Get a page block item based on index slot
    /// </summary>
    public Span<byte> Get(byte index, bool readOnly)
    {
        // get read
        var span = readOnly ? _readBuffer.Memory.Span :
            _writeBuffer != null ? _writeBuffer.Memory.Span :
            _readBuffer.Memory.Span;

        // read slot address
        var positionAddr = CalcPositionAddr(index);
        var lengthAddr = CalcLengthAddr(index);

        // read segment position/length
        var position = span[positionAddr..2].ReadUInt16();
        var length = span[lengthAddr..2].ReadUInt16();

        // return buffer slice with content only data
        return span[position..length];
    }

    /// <summary>
    /// Get a new page segment for this length content
    /// </summary>
    public Span<byte> Insert(ushort bytesLength, out byte index)
    {
        index = byte.MaxValue;

        return this.InternalInsert(bytesLength, ref index);
    }

    /// <summary>
    /// Get a new page segment for this length content using fixed index
    /// </summary>
    private Span<byte> InternalInsert(ushort bytesLength, ref byte index)
    {
        // initialize dirty buffer and dirty header (once)
        this.InitializeWrite();

        var span = _writeBuffer.Memory.Span;
        var header = _headerWrite;

        var isNewInsert = index == byte.MaxValue;

        if (!(header.FreeBytes >= bytesLength + (isNewInsert ? BlockPageHeader.SLOT_SIZE : 0)))
        {
            throw ERR_INVALID_FREE_SPACE_PAGE(this.PageID, header.FreeBytes, bytesLength + (isNewInsert ? BlockPageHeader.SLOT_SIZE : 0));
        }

        // calculate how many continuous bytes are avaiable in this page
        var continuousBlocks = header.FreeBytes - header.FragmentedBytes - (isNewInsert ? BlockPageHeader.SLOT_SIZE : 0);

        ENSURE(continuousBlocks == PAGE_SIZE - header.NextFreePosition - header.FooterSize - (isNewInsert ? BlockPageHeader.SLOT_SIZE : 0), "continuosBlock must be same as from NextFreePosition");

        // if continuous blocks are not big enough for this data, must run page defrag
        if (bytesLength > continuousBlocks)
        {
            this.Defrag();
        }

        // if index is new insert segment, must request for new Index
        if (index == byte.MaxValue)
        {
            // get new free index must run after defrag
            index = header.GetFreeIndex(span);
        }

        if (index > header.HighestIndex || header.HighestIndex == byte.MaxValue)
        {
            ENSURE(index == (byte)(header.HighestIndex + 1), "new index must be next highest index");

            header.HighestIndex = index;
        }

        // get segment addresses
        var positionAddr = CalcPositionAddr(index);
        var lengthAddr = CalcLengthAddr(index);

        ENSURE(span[positionAddr..].ReadUInt16() == 0, "slot position must be empty before use");
        ENSURE(span[lengthAddr..].ReadUInt16() == 0, "slot length must be empty before use");

        // get next free position in page
        var position = header.NextFreePosition;

        // write this page position in my position address
        span[positionAddr..].WriteUInt16(position);

        // write page segment length in my length address
        span[lengthAddr..].WriteUInt16(bytesLength);

        // update next free position and counters
        header.ItemsCount++;
        header.UsedBytes += bytesLength;
        header.NextFreePosition += bytesLength;

        ENSURE(position + bytesLength <= (PAGE_SIZE - (header.HighestIndex + 1) * BlockPageHeader.SLOT_SIZE), "new buffer slice could not override footer area");

        // create page segment based new inserted segment
        return span[position..(position + bytesLength)];
    }

    /// <summary>
    /// Remove index slot about this page block
    /// </summary>
    public void Delete(byte index)
    {
        // initialize dirty buffer and dirty header (once)
        this.InitializeWrite();

        // get span and header instance (dirty)
        var span = _writeBuffer.Memory.Span;
        var header = _headerWrite;

        // read block position on index slot
        var positionAddr = CalcPositionAddr(index);
        var lengthAddr = CalcLengthAddr(index);

        var position = span[positionAddr..].ReadUInt16();
        var length = span[lengthAddr..].ReadUInt16();

        ENSURE(this.IsValidPos(position), "invalid segment position");
        ENSURE(this.IsValidLen(length), "invalid segment length");

        // clear both position/length
        span[positionAddr..].WriteUInt16(0);
        span[lengthAddr..].WriteUInt16(0);

        // add as free blocks
        header.ItemsCount--;
        header.UsedBytes -= length;

        // clean block area with \0
        span[position..(position + length)].Fill(0);

        // check if deleted segment are at end of page
        var isLastSegment = (position + length == header.NextFreePosition);

        if (isLastSegment)
        {
            // update next free position with this deleted position
            header.NextFreePosition = position;
        }
        else
        {
            // if segment is in middle of the page, add this blocks as fragment block
            header.FragmentedBytes += length;
        }

        // if deleted if are HighestIndex, update HighestIndex
        if (header.HighestIndex == index)
        {
            this.UpdateHighestIndex();
        }

        // reset start index (used in GetFreeIndex)
        header.ResetStartIndex();

        // if there is no more blocks in page, clean FragmentedBytes and NextFreePosition
        if (header.ItemsCount == 0)
        {
            ENSURE(header.HighestIndex == byte.MaxValue, "if there is no items, HighestIndex must be clear");
            ENSURE(header.UsedBytes == 0, "should be no bytes used in clean page");
            DEBUG(span[PAGE_HEADER_SIZE..PAGE_CONTENT_SIZE].IsFullZero(), "all content area must be 0");

            header.NextFreePosition = PAGE_HEADER_SIZE;
            header.FragmentedBytes = 0;
        }
    }

    /// <summary>
    /// Update segment bytes with new data. Current page must have bytes enougth for this new size. Index will not be changed
    /// Update will try use same segment to store. If not possible, write on end of page (with possible Defrag operation)
    /// </summary>
    public Span<byte> Update(byte index, ushort bytesLength)
    {
        ENSURE(bytesLength > 0, "must update more than 0 bytes");

        // initialize dirty buffer and dirty header (once)
        this.InitializeWrite();

        // get span and header instance (dirty)
        var span = _writeBuffer.Memory.Span;
        var header = _headerWrite;

        // read slot address
        var positionAddr = CalcPositionAddr(index);
        var lengthAddr = CalcLengthAddr(index);

        // read segment position/length
        var position = span[positionAddr..].ReadUInt16();
        var length = span[lengthAddr..].ReadUInt16();

        ENSURE(this.IsValidPos(position), "invalid segment position");
        ENSURE(this.IsValidLen(length), "invalid segment length");

        // check if deleted segment are at end of page
        var isLastSegment = (position + length == header.NextFreePosition);

        // best situation: same length
        if (bytesLength == length)
        {
            return span[position..(position + length)];
        }
        // when new length are less than original length (will fit in current segment)
        else if (bytesLength < length)
        {
            var diff = (ushort)(length - bytesLength); // bytes removed (should > 0)

            if (isLastSegment)
            {
                // if is at end of page, must get back unused blocks 
                header.NextFreePosition -= diff;
            }
            else
            {
                // is this segment are not at end, must add this as fragment
                header.FragmentedBytes += diff;
            }

            // less blocks will be used
            header.UsedBytes -= diff;

            // update length
            span[lengthAddr..].WriteUInt16(bytesLength);

            // clear fragment bytes
            var clearStart = position + bytesLength;
            var clearEnd = clearStart + diff;

            span[clearStart..clearEnd].Fill(0);

            return span[position..(position + bytesLength)];
        }
        // when new length are large than current segment must remove current item and add again
        else
        {
            // clear current block
            span[position..(position + length)].Fill(0);

            header.ItemsCount--;
            header.UsedBytes -= length;

            if (isLastSegment)
            {
                // if segment is end of page, must update next free position to current segment position
                header.NextFreePosition = position;
            }
            else
            {
                // if segment is on middle of page, add content length as fragment bytes
                header.FragmentedBytes += length;
            }

            // clear slot index position/length
            span[positionAddr..].WriteUInt16(0);
            span[lengthAddr..].WriteUInt16(0);

            // call insert
            return this.InternalInsert(bytesLength, ref index);
        }
    }

    /// <summary>
    /// Defrag method re-organize all byte data content removing all fragmented data. This will move all page blocks
    /// to create a single continuous content area (just after header area). No index block will be changed (only positions)
    /// </summary>
    public void Defrag()
    {
        // initialize dirty buffer and dirty header (once)
        this.InitializeWrite();

        // get span and header instance (dirty)
        var span = _writeBuffer.Memory.Span;
        var header = _headerWrite;

        ENSURE(header.FragmentedBytes > 0, "do not call this when page has no fragmentation");
        ENSURE(header.HighestIndex < byte.MaxValue, "there is no items in this page to run defrag");

        //LOG($"defrag page #{this.PageID} (fragments: {this.FragmentedBytes})", "DISK");

        // first get all blocks inside this page sorted by position (position, index)
        var blocks = new SortedList<ushort, byte>();

        // use int to avoid byte overflow
        for (int index = 0; index <= header.HighestIndex; index++)
        {
            var positionAddr = CalcPositionAddr((byte)index);
            var position = span[positionAddr..].ReadUInt16();

            // get only used index
            if (position != 0)
            {
                ENSURE(this.IsValidPos(position), "invalid segment position");

                // sort by position
                blocks.Add(position, (byte)index);
            }
        }

        // here first block position
        var next = (ushort)PAGE_HEADER_SIZE;

        // now, list all segments order by Position
        foreach (var slot in blocks)
        {
            var index = slot.Value;
            var position = slot.Key;

            // get segment length
            var lengthAddr = CalcLengthAddr(index);
            var length = span[lengthAddr..].ReadUInt16();

            ENSURE(this.IsValidLen(length), "invalid segment length");

            // if current segment are not as excpect, copy buffer to right position (excluding empty space)
            if (position != next)
            {
                ENSURE(position > next, "current segment position must be greater than current empty space");

                throw new NotImplementedException("revisar em debug");

                // copy from original position into new (correct) position
                var source = span[position..(position + length)];
                var dest = span[next..(next + length)];

                source.CopyTo(dest);

                // update index slot with this new block position
                var positionAddr = CalcPositionAddr(index);

                // update position in footer
                span[positionAddr..].WriteUInt16(next);
            }

            next += length;
        }

        // fill all non-used content area with 0
        var endContent = PAGE_SIZE - header.FooterSize;

        span[next..endContent].Fill(0);

        // clear fragment blocks (page are in a continuous segment)
        header.FragmentedBytes = 0;
        header.NextFreePosition = next;
    }

    /// <summary>
    /// Update HighestIndex based on current HighestIndex (step back looking for next used slot)
    /// Used only in Delete() operation
    /// </summary>
    private void UpdateHighestIndex()
    {
        // get span and header instance (dirty)
        var span = _writeBuffer.Memory.Span;
        var header = _headerWrite;

        ENSURE(header.HighestIndex < byte.MaxValue, "can run only if contains a valid HighestIndex");

        // if current index is 0, clear index
        if (header.HighestIndex == 0)
        {
            header.HighestIndex = byte.MaxValue;
            return;
        }

        // start from current - 1 to 0 (should use "int" because for use ">= 0")
        for (int index = header.HighestIndex - 1; index >= 0; index--)
        {
            var positionAddr = CalcPositionAddr((byte)index);
            var position = span[positionAddr..].ReadUInt16();

            if (position != 0)
            {
                ENSURE(this.IsValidPos(position), "invalid segment position");

                header.HighestIndex = (byte)index;
                return;
            }
        }

        // there is no more slots used
        header.HighestIndex = byte.MaxValue;
    }

    #endregion

    #region Static Helpers

    /// <summary>
    /// Get buffer offset position where one page segment length are located (based on index slot)
    /// </summary>
    public static int CalcPositionAddr(byte index) => PAGE_SIZE - ((index + 1) * BlockPageHeader.SLOT_SIZE) + 2;

    /// <summary>
    /// Get buffer offset position where one page segment length are located (based on index slot)
    /// </summary>
    public static int CalcLengthAddr(byte index) => PAGE_SIZE - ((index + 1) * BlockPageHeader.SLOT_SIZE);

    /// <summary>
    /// Checks if segment position has a valid value (used for DEBUG)
    /// </summary>
    private bool IsValidPos(ushort position) => position >= PAGE_HEADER_SIZE && position < (PAGE_SIZE - _headerWrite.FooterSize);

    /// <summary>
    /// Checks if segment length has a valid value (used for DEBUG)
    /// </summary>
    private bool IsValidLen(ushort length) => length > 0 && length <= (PAGE_SIZE - PAGE_HEADER_SIZE - _headerWrite.FooterSize);

    #endregion

}
