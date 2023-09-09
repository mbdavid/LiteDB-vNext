namespace LiteDB.Engine;

unsafe internal partial struct PageMemory // PageMemory.Segment
{
    public static PageSegment* GetSegmentPtr(PageMemory* page, ushort index)
    {
        var segmentOffset = PAGE_SIZE - (index * sizeof(PageSegment));

        var segment = (PageSegment*)((nint)page + segmentOffset);

        return segment;
    }

    public static PageSegment* InsertSegment(PageMemory* page, ushort bytesLength, ushort index, bool isNewInsert, out bool defrag, out ExtendPageValue newPageValue)
    {
        ENSURE(index != ushort.MaxValue, new { bytesLength, index, isNewInsert });
        ENSURE(bytesLength % 8 == 0 && bytesLength > 0, new { bytesLength, index, isNewInsert });

        // mark page as dirty
        page->IsDirty = true;

        // get initial page value to check for changes
        var initialPageValue = page->ExtendPageValue;

        //TODO: converter em um ensure
        if (!(page->FreeBytes >= bytesLength + (isNewInsert ? sizeof(PageSegment) : 0)))
        {
            throw ERR_INVALID_FREE_SPACE_PAGE(page->PageID, page->FreeBytes, bytesLength + (isNewInsert ? sizeof(PageSegment) : 0));
        }

        // calculate how many continuous bytes are available in this page
        var continuousBlocks = page->FreeBytes - page->FragmentedBytes - (isNewInsert ? sizeof(PageSegment) : 0);

        ENSURE(continuousBlocks == PAGE_SIZE - page->NextFreeLocation - page->FooterSize - (isNewInsert ? sizeof(PageSegment) : 0), "ContinuosBlock must be same as from NextFreePosition",
            new { continuousBlocks, isNewInsert });

        // if continuous blocks are not big enough for this data, must run page defrag
        defrag = bytesLength > continuousBlocks;

        if (defrag)
        {
            //this.Defrag(page);
            throw new NotImplementedException();
        }

        if (index > page->HighestIndex || page->HighestIndex == ushort.MaxValue)
        {
            page->HighestIndex = index;
        }

        // get segment addresses
        var segment = PageMemory.GetSegmentPtr(page, index);

        ENSURE(segment->IsEmpty, "segment must be free in insert", new { location = segment->Location, length = segment->Length });

        // get next free location in page
        var location = page->NextFreeLocation;

        // update segment footer
        segment->Location = location;
        segment->Length = bytesLength;

        // update next free location and counters
        page->ItemsCount++;
        page->UsedBytes += bytesLength;
        page->NextFreeLocation += bytesLength;

        ENSURE(location + bytesLength <= (PAGE_SIZE - (page->HighestIndex + 1) * sizeof(PageSegment)), "New buffer slice could not override footer area",
            new { location, bytesLength});

        // check for change on extend pageValue
        newPageValue = initialPageValue == page->ExtendPageValue ? ExtendPageValue.NoChange : page->ExtendPageValue;

        // create page segment based new inserted segment
        return segment;
    }

    /// <summary>
    /// Remove index slot about this page segment. Returns deleted page segment
    /// </summary>
    public static void DeleteSegment(PageMemory* page, ushort index, out ExtendPageValue newPageValue)
    {
        // mark page as dirty
        page->IsDirty = true;

        // get initial page value to check for changes
        var initialPageValue = page->ExtendPageValue;

        // read block position on index slot
        var segment = PageMemory.GetSegmentPtr(page, index);

        // add as free blocks
        page->ItemsCount--;
        page->UsedBytes -= segment->Length;

        // clean block area with \0
        var pageContent = (byte*)((nint)page + segment->Location); // position on page

        MarshalEx.FillZero(pageContent, segment->Length);

        // check if deleted segment are at end of page
        var isLastSegment = (segment->EndLocation == page->NextFreeLocation);

        if (isLastSegment)
        {
            // update next free location with this deleted segment
            page->NextFreeLocation = segment->Location;
        }
        else
        {
            // if segment is in middle of the page, add this blocks as fragment block
            page->FragmentedBytes += segment->Length;
        }

        // if deleted if are HighestIndex, update HighestIndex
        if (page->HighestIndex == index)
        {
            PageMemory.UpdateHighestIndex(page);
        }

        // if there is no more blocks in page, clean FragmentedBytes and NextFreePosition
        if (page->ItemsCount == 0)
        {
            ENSURE(page->HighestIndex == ushort.MaxValue, "if there is no items, HighestIndex must be clear");
            ENSURE(page->UsedBytes == 0, "should be no bytes used in clean page");

            page->NextFreeLocation = PAGE_HEADER_SIZE;
            page->FragmentedBytes = 0;
        }

        // clear both location/length
        segment->Location = 0;
        segment->Length = 0;

        // check for change on extend pageValue
        newPageValue = initialPageValue == page->ExtendPageValue ? ExtendPageValue.NoChange : page->ExtendPageValue;
    }

    /// <summary>
    /// </summary>
    public static PageSegment* UpdateSegment(PageMemory* page, ushort index, ushort bytesLength, out bool defrag, out ExtendPageValue newPageValue)
    {
        ENSURE(bytesLength % 8 == 0 && bytesLength > 0, new { bytesLength });

        // mark page as dirty
        page->IsDirty = true;

        // get initial page value to check for changes
        var initialPageValue = page->ExtendPageValue;

        // read page segment
        var segment = PageMemory.GetSegmentPtr(page, index);

        ENSURE(page->FreeBytes - segment->Length >= bytesLength, $"There is no free space in page {page->PageID} for {bytesLength} bytes required (free space: {page->FreeBytes}");

        // check if current segment are at end of page
        var isLastSegment = (segment->EndLocation == page->NextFreeLocation);

        // best situation: same length
        if (bytesLength == segment->Length)
        {
            defrag = false;
            newPageValue = ExtendPageValue.NoChange;

            return segment;
        }
        // when new length are less than original length (will fit in current segment)
        else if (bytesLength < segment->Length)
        {
            var diff = (ushort)(segment->Length - bytesLength); // bytes removed (should > 0)

            if (isLastSegment)
            {
                // if is at end of page, must get back unused blocks 
                page->NextFreeLocation -= diff;
            }
            else
            {
                // is this segment are not at end, must add this as fragment
                page->FragmentedBytes += diff;
            }

            // less blocks will be used
            page->UsedBytes -= diff;

            // update length
            segment->Length = bytesLength;

            // clear fragment bytes
            var fragment = (byte*)((nint)page + segment->Location + bytesLength);

            MarshalEx.FillZero(fragment, diff);

            defrag = false;

            // check for change on extend pageValue
            newPageValue = initialPageValue == page->ExtendPageValue ? ExtendPageValue.NoChange : page->ExtendPageValue;

            return segment;
        }
        // when new length are large than current segment must remove current item and add again
        else
        {
            // clear current block
            var dataPtr = (byte*)((nint)page + segment->Location);

            MarshalEx.FillZero(dataPtr, segment->Length);

            page->ItemsCount--;
            page->UsedBytes -= segment->Length;

            if (isLastSegment)
            {
                // if segment is end of page, must update next free location to current segment location
                page->NextFreeLocation = segment->Location;
            }
            else
            {
                // if segment is on middle of page, add content length as fragment bytes
                page->FragmentedBytes += segment->Length;
            }

            // clear slot index location/length
            segment->Location = 0;
            segment->Length = 0;

            // call insert
            return InsertSegment(page, bytesLength, index, false, out defrag, out newPageValue);
        }
    }

    /// <summary>
    /// Defrag method re-organize all byte data content removing all fragmented data. This will move all page blocks
    /// to create a single continuous content area (just after header area). No index block will be changed (only positions)
    /// </summary>
    private static void Defrag(PageMemory* page)
    {
        ENSURE(page->FragmentedBytes > 0, "do not call this when page has no fragmentation");
        ENSURE(page->HighestIndex < byte.MaxValue, "there is no items in this page to run defrag");

        // first get all blocks inside this page sorted by location (location, index)
        var blocks = new SortedList<ushort, ushort>(page->ItemsCount);

        // get first segment
        var segment = PageMemory.GetSegmentPtr(page, 0);

        // read all segments from footer
        for (ushort index = 0; index <= page->HighestIndex; index++)
        {
            // get only used index
            if (segment->Location != 0)
            {
                // sort by position
                blocks.Add(segment->Location, index);
            }

            segment--;
        }

        // here first block position
        var next = (ushort)PAGE_HEADER_SIZE;

        // now, list all segments order by location
        foreach (var slot in blocks)
        {
            var index = slot.Value;
            var location = slot.Key;

            // get segment address
            var addr = PageMemory.GetSegmentPtr(page, index);

            // if current segment are not as excpect, copy buffer to right position (excluding empty space)
            if (location != next)
            {
                ENSURE(location > next, "current segment position must be greater than current empty space", new { location, next });

                // copy from original location into new (correct) location
                var sourceSpan = new Span<byte>((byte*)((nint)page + location), addr->Length);
                var destSpan = new Span<byte>((byte*)((nint)page + next), addr->Length);

                sourceSpan.CopyTo(destSpan);

                // update new location for this index (on footer page)
                addr->Location = next;
            }

            next += addr->Length;
        }

        // fill all non-used content area with 0
        var endContent = PAGE_SIZE - page->FooterSize;

        var nextPtr = (byte*)((nint)page + next);
        var contentLength = endContent - next;

        MarshalEx.FillZero(nextPtr, contentLength);

        // clear fragment blocks (page are in a continuous segment)
        page->FragmentedBytes = 0;
        page->NextFreeLocation = next;
    }


    /// <summary>
    /// Get a free index slot in this page
    /// </summary>
    public static ushort GetFreeIndex(PageMemory* page)
    {
        // get first pointer do 0 index segment
        var segment = PageMemory.GetSegmentPtr(page, 0);

        // check for all slot area to get first empty slot [safe for byte loop]
        for (var index = 0; index <= page->HighestIndex; index++)
        {
            if (segment->Location == 0)
            {
                return (ushort)index;
            }

            segment--;
        }

        return (byte)(page->HighestIndex + 1);
    }


    /// <summary>
    /// Update HighestIndex based on current HighestIndex (step back looking for next used slot)
    /// * Used only in Delete() operation *
    /// </summary>
    private static void UpdateHighestIndex(PageMemory* page)
    {
        // if current index is 0, clear index
        if (page->HighestIndex == 0)
        {
            page->HighestIndex = byte.MaxValue;
            return;
        }

        var highestIndex = (ushort)(page->HighestIndex - 1);
        var segment = PageMemory.GetSegmentPtr(page, highestIndex);

        // start from current - 1 to 0 (should use "int" because for use ">= 0")
        for (int index = highestIndex; index >= 0; index--)
        {
            if (segment->Location != 0)
            {
                page->HighestIndex = (ushort)index;
                return;
            }

            segment++;
        }

        // there is no more slots used
        page->HighestIndex = ushort.MaxValue;
    }

}
