namespace LiteDB.Engine;

/// <summary>
/// Represent a single allocation map page with 1.632 extends and 13.056 pages pointer
/// </summary>
internal class AllocationMapPage : BasePage
{
    /// <summary>
    /// Get how many extends exists in this page
    /// </summary>
    public int ExtendsCount => AMP_EXTEND_COUNT - _emptyExtends.Count;

    private readonly Queue<int> _emptyExtends = new();

    /// <summary>
    /// Create new AllocationMapPage instance
    /// </summary>
    public AllocationMapPage(uint pageID)
        : base(pageID, PageType.AllocationMap)
    {
    }

    /// <summary>
    /// </summary>
    public AllocationMapPage(IMemoryOwner<byte> buffer)
        : base(buffer)
    {
        var span = _readBuffer.Memory.Span;

        for(var i = 0; i < AMP_EXTEND_COUNT; i++)
        {
            var position = PAGE_HEADER_SIZE + (i * AMP_EXTEND_SIZE);

            // check if empty colID (means empty extend)
            if (span[position] == 0)
            {
                DEBUG(span[position..(position + AMP_EXTEND_SIZE)].IsFullZero(), $"all page extend allocation map should be empty at {position}");

                _emptyExtends.Enqueue(i);
            }
        }
    }

    /// <summary>
    /// Update
    /// </summary>
    public void UpdateMap(int extendIndex, int pageIndex, PageType pageType, byte colID, ushort freeSpace)
    {
        this.InitializeWrite();
        // usado no foreach depois de salvar em disco as paginas
        var span = _writeBuffer.Memory.Span;

        ENSURE(span[PAGE_HEADER_SIZE + (extendIndex * AMP_EXTEND_SIZE)] == colID, "this map page don't bellow to this extend collection");

        // get byte position for 2 pages (even and odd)
        var position = PAGE_HEADER_SIZE + (extendIndex * AMP_EXTEND_SIZE) + 1 + (pageIndex / 2);

        var even = pageIndex % 2 == 0;

        var pageTypeSlot = (PageType)(span[position] & (even ? 0b1100_0000 : 0b0000_1100));

        ENSURE(pageTypeSlot == pageType || pageTypeSlot == PageType.Empty, "page type doesn't match in map");

        if (pageTypeSlot == PageType.Empty)
        {
            pageTypeSlot = pageType;
        }

        var freeSpaceSlot = 0;

        if (pageTypeSlot == PageType.Index)
        {
            freeSpaceSlot = freeSpace > AMP_INDEX_PAGE_SPACE ? 0b00 : 0b01;
        }
        else if (pageTypeSlot == PageType.Data)
        {
            if (freeSpace > AMP_DATA_PAGE_SPACE_00)
            {
                freeSpaceSlot = 0b00;
            }
            else if (freeSpace > AMP_DATA_PAGE_SPACE_01)
            {
                freeSpaceSlot = 0b01;
            }
            else if (freeSpace > AMP_DATA_PAGE_SPACE_10)
            {
                freeSpaceSlot = 0b10;
            }
            else
            {
                freeSpaceSlot = 0b11;
            }
        }

        // get pageType + freeSpace in 4 bits 
        var pageInfo = ((byte)pageTypeSlot << 2) | freeSpaceSlot;

        // update left/right part of byte (page even or odd)
        var byteData = even ?
            ((pageInfo << 4) | (span[position] & 0b0000_1111)) :
            (pageInfo | (span[position] & 0b1111_0000));

        span[position] = (byte)byteData;

    }

    public uint GetFreePageID(byte coldID, PageType type, int length)
    {
        this.InitializeWrite();



        return 0;
    }

    public uint NewPageID(byte colID, PageType type)
    {
        this.InitializeWrite();



        return 0;
    }

    private int AddExtend(byte colID)
    {
        var span = _writeBuffer.Memory.Span;

        ENSURE(_emptyExtends.Count > 0, "must have at least 1 empty extend on map page");

        var extendID = _emptyExtends.Dequeue();
        var position = AMP_EXTEND_SIZE; //TODO:sombrio, calcular

        span[position] = colID;

        return extendID;
    }

    /// <summary>
    /// Find a pageType inside an extend with minimal size to fit in length. Can return a empty page if not found in any other pages
    /// </summary>
    private uint FindFreePageID(Span<byte> span, int extendIndex, PageType pageType, int length)
    {
        ENSURE(length < AMP_DATA_PAGE_SPACE_00, $"if need length bigger than {AMP_DATA_PAGE_SPACE_00} need create new page");

        var extendPosition = PAGE_HEADER_SIZE + (extendIndex * AMP_EXTEND_SIZE);
        var pageID = (uint)(this.PageID + (extendIndex * AMP_EXTEND_SIZE) + 1);
        var empty = uint.MaxValue;

        for (var i = 0; i < AMP_EXTEND_SIZE; i++)
        {
            var even = i % 2 == 0;
            var position = extendPosition + 1 + (i / 2); // add collection byte
            var data = span[position] & (even ? 0b1111_0000 : 0b0000_1111);

            var slotPageType = (PageType)((data & 0b1100) >> 2);

            if (slotPageType == pageType)
            {
                var slotFreeSpace = data & 0b0011;

                if (pageType == PageType.Index)
                {
                    if (slotFreeSpace == 0)
                    {
                        return (uint)(pageID + i);
                    }
                }
                else if (pageType == PageType.Data)
                {
                    if (slotFreeSpace == 0b00)
                    {
                        return (uint)(pageID + i);
                    }
                    else if (slotFreeSpace == 0b01 && length < AMP_DATA_PAGE_SPACE_00)
                    {
                        return (uint)(pageID + i);
                    }
                    else if (slotFreeSpace == 0b10 && length < AMP_DATA_PAGE_SPACE_01)
                    {
                        return (uint)(pageID + i);
                    }
                    else if (slotFreeSpace == 0b11)
                    {
                        continue;
                    }
                }
            }
            else if (slotPageType == PageType.Empty && empty == uint.MaxValue)
            {
                empty = (uint)(pageID + i); // return Empty page to used as new page
            }
        }

        // will return first empty pageID or MaxValue if not found
        return empty;
    }


    #region Static Helpers

    public static bool IsAllocationMapPageID(uint pageID)
    {
        var pfsId = pageID - AMP_FIRST_PAGE_ID;

        return pfsId % AMP_STEP_SIZE == 0;
    }

    public static void GetLocation(uint pageID,
        out uint pfsPageID, // AllocationMapID (começa em 0, 1, 2, 3)
        out int extendIndex, // ExtendIndex (começa em 0, 1, ..., 1631, 0, 1, ...)
        out int pageIndex) // PageIndex inside extend content (0, 1, 2, 3, 4, 5, 6, 7)
    {
        // test if is non-mapped page in PFS
        if (pageID <= AMP_FIRST_PAGE_ID || IsAllocationMapPageID(pageID))
        {
            pfsPageID = uint.MaxValue;
            extendIndex = -1;
            pageIndex = -1;

            return;
        }

        var pfsId = pageID - AMP_FIRST_PAGE_ID;
        var aux = pfsId - (pfsId / AMP_STEP_SIZE + 1);

        pfsPageID = pfsId / AMP_STEP_SIZE * AMP_STEP_SIZE + AMP_FIRST_PAGE_ID;
        extendIndex = (int)(aux / AMP_EXTEND_SIZE) % (PAGE_CONTENT_SIZE / AMP_BYTES_PER_EXTEND);
        pageIndex = (int)(aux % AMP_EXTEND_SIZE);

        ENSURE(IsAllocationMapPageID(pfsPageID), $"Page {pfsPageID} is not a valid PFS");
        ENSURE(extendIndex < AMP_EXTEND_COUNT, $"Extend {extendIndex} must be less than {AMP_EXTEND_COUNT}");
        ENSURE(pageIndex < AMP_EXTEND_SIZE, $"Page index {pageIndex} must be less than {AMP_EXTEND_SIZE}");
    }

    #endregion

}
