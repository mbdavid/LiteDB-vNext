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
    public BlockPage(Memory<byte> buffer, uint pageID, PageType pageType, byte colID)
        : base(buffer, pageID, pageType)
    {
        var span = buffer.Span;

        // ColID never change
        this.ColID = colID;

        // initialize an empty header
        _header = new BlockPageHeader();
    }

    /// <summary>
    /// Load BlockPage from buffer memory
    /// </summary>
    public BlockPage(Memory<byte> buffer)
        : base(buffer)
    {
        var span = buffer.Span;

        // initialize variables with buffer data
        this.ColID = span.ReadByte(P_COL_ID);
        this.TransactionID = span.ReadUInt32(P_TRANSACTION_ID);
        this.IsConfirmed = span.ReadBool(P_IS_CONFIRMED);

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
        span.Write(this.ColID, P_COL_ID);
        span.Write(this.TransactionID, P_TRANSACTION_ID);
        span.Write(this.IsConfirmed, P_IS_CONFIRMED);

        // update header instance
        _headerWrite.Update(span);

        return buffer;
    }

    #region Blocks Operations

    /// <summary>
    /// Get read or write buffer
    /// </summary>
    private Span<byte> GetSpan(bool readOnly)
    {
        if (readOnly) return _buffer.Span;

        if (_headerWrite != null) return _bufferWrite.Memory.Span;

        return _buffer.Span;
    }

    /// <summary>
    /// Get a page block item based on index slot
    /// </summary>
    public Span<byte> Get(byte index, bool readOnly)
    {
        var span = this.GetSpan(readOnly);

        // read slot address
        var positionAddr = CalcPositionAddr(index);
        var lengthAddr = CalcLengthAddr(index);

        // read segment position/length
        var position = span.ReadUInt16(positionAddr);
        var length = span.ReadUInt16(lengthAddr);

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

        var span = _bufferWrite.Memory.Span;
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
            //**this.Defrag();
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

        ENSURE(span.ReadUInt16(positionAddr) == 0, "slot position must be empty before use");
        ENSURE(span.ReadUInt16(lengthAddr) == 0, "slot length must be empty before use");

        // get next free position in page
        var position = header.NextFreePosition;

        // write this page position in my position address
        span.Write(position, positionAddr);

        // write page segment length in my length address
        span.Write(bytesLength, lengthAddr);

        // update next free position and counters
        header.ItemsCount++;
        header.UsedBytes += bytesLength;
        header.NextFreePosition += bytesLength;

        ENSURE(position + bytesLength <= (PAGE_SIZE - (header.HighestIndex + 1) * BlockPageHeader.SLOT_SIZE), "new buffer slice could not override footer area");

        // create page segment based new inserted segment
        return span[position..bytesLength];
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

    #endregion

}
