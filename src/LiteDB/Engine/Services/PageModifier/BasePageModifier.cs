namespace LiteDB.Engine;

[AutoInterface]
unsafe internal class BasePageModifier : IBasePageModifier
{
    protected PageSegment* Insert(PageMemory* pagePtr, ushort bytesLength, ushort index, bool isNewInsert)
    {
        ENSURE(index != ushort.MaxValue, new { bytesLength, index, isNewInsert });

        // mark page as dirty
        pagePtr->IsDirty = true;

        //TODO: converter em um ensure
        if (!(pagePtr->FreeBytes >= bytesLength + (isNewInsert ? sizeof(PageSegment) : 0)))
        {
            throw ERR_INVALID_FREE_SPACE_PAGE(pagePtr->PageID, pagePtr->FreeBytes, bytesLength + (isNewInsert ? sizeof(PageSegment) : 0));
        }

        // calculate how many continuous bytes are available in this page
        var continuousBlocks = pagePtr->FreeBytes - pagePtr->FragmentedBytes - (isNewInsert ? sizeof(PageSegment) : 0);

        ENSURE(continuousBlocks == PAGE_SIZE - pagePtr->NextFreeLocation - pagePtr->FooterSize - (isNewInsert ? sizeof(PageSegment) : 0), "ContinuosBlock must be same as from NextFreePosition",
            new { continuousBlocks, isNewInsert });

        // if continuous blocks are not big enough for this data, must run page defrag
        if (bytesLength > continuousBlocks)
        {
            //this.Defrag(page);
        }

        if (index > pagePtr->HighestIndex || pagePtr->HighestIndex == ushort.MaxValue)
        {
            ENSURE(index == (ushort)(pagePtr->HighestIndex + 1), "new index must be next highest index", new { index });

            pagePtr->HighestIndex = index;
        }

        // get segment addresses
        var segmentPtr = PageSegment.GetSegment(pagePtr, index);

        ENSURE(segmentPtr->IsEmpty, "segment must be free in insert", new { location = segmentPtr->Location, length = segmentPtr->Length });

        // get next free location in page
        var location = pagePtr->NextFreeLocation;

        // update segment footer
        segmentPtr->Location = location;
        segmentPtr->Length = bytesLength;

        // update next free location and counters
        pagePtr->ItemsCount++;
        pagePtr->UsedBytes += bytesLength;
        pagePtr->NextFreeLocation += bytesLength;

        ENSURE(location + bytesLength <= (PAGE_SIZE - (pagePtr->HighestIndex + 1) * PageHeader.SLOT_SIZE), "New buffer slice could not override footer area",
            new { location, bytesLength});

        // create page segment based new inserted segment
        return segmentPtr;
    }

    /// <summary>
    /// Remove index slot about this page segment. Returns deleted page segment
    /// </summary>
    public void Delete(PageMemory* pagePtr, ushort index)
    {
        // mark page as dirty
        pagePtr->IsDirty = true;

        // read block position on index slot
        var segmentPtr = PageSegment.GetSegment(pagePtr, index);

        // clear both location/length
        segmentPtr->Location = 0;
        segmentPtr->Length = 0;

        // add as free blocks
        pagePtr->ItemsCount--;
        pagePtr->UsedBytes -= segmentPtr->Length;

        // clean block area with \0
        var dataPtr = (byte*)((nint)pagePtr + segmentPtr->Location); // position on page
        MarshalEx.FillZero(dataPtr, segmentPtr->Length);

        // check if deleted segment are at end of page
        var isLastSegment = (segmentPtr->EndLocation == pagePtr->NextFreeLocation);

        if (isLastSegment)
        {
            // update next free location with this deleted segment
            pagePtr->NextFreeLocation = segmentPtr->Location;
        }
        else
        {
            // if segment is in middle of the page, add this blocks as fragment block
            pagePtr->FragmentedBytes += segmentPtr->Length;
        }

        // if deleted if are HighestIndex, update HighestIndex
        if (pagePtr->HighestIndex == index)
        {
            UpdateHighestIndex(pagePtr);
        }

        // reset start index (used in GetFreeIndex)
        //****header.ResetStartIndex();

        // if there is no more blocks in page, clean FragmentedBytes and NextFreePosition
        if (pagePtr->ItemsCount == 0)
        {
            ENSURE(pagePtr->HighestIndex == byte.MaxValue, "if there is no items, HighestIndex must be clear");
            ENSURE(pagePtr->UsedBytes == 0, "should be no bytes used in clean page");

            pagePtr->NextFreeLocation = PAGE_HEADER_SIZE;
            pagePtr->FragmentedBytes = 0;
        }
    }

    /// <summary>
    /// </summary>
    protected PageSegment* Update(PageMemory* pagePtr, ushort index, ushort bytesLength)
    {
        ENSURE(bytesLength > 0, "Must update more than 0 bytes", new { bytesLength });

        // mark page as dirty
        pagePtr->IsDirty = true;

        // read page segment
        var segmentPtr = PageSegment.GetSegment(pagePtr, index);

        ENSURE(pagePtr->FreeBytes - segmentPtr->Length >= bytesLength, $"There is no free space in page {pagePtr->PageID} for {bytesLength} bytes required (free space: {pagePtr->FreeBytes}");

        // check if current segment are at end of page
        var isLastSegment = (segmentPtr->EndLocation == pagePtr->NextFreeLocation);

        // best situation: same length
        if (bytesLength == segmentPtr->Length)
        {
            return segmentPtr;
        }
        // when new length are less than original length (will fit in current segment)
        else if (bytesLength < segmentPtr->Length)
        {
            var diff = (ushort)(segmentPtr->Length - bytesLength); // bytes removed (should > 0)

            if (isLastSegment)
            {
                // if is at end of page, must get back unused blocks 
                pagePtr->NextFreeLocation -= diff;
            }
            else
            {
                // is this segment are not at end, must add this as fragment
                pagePtr->FragmentedBytes += diff;
            }

            // less blocks will be used
            pagePtr->UsedBytes -= diff;

            // update length
            segmentPtr->Length = bytesLength;

            // clear fragment bytes
            var fragmentPtr = (byte*)((nint)pagePtr + segmentPtr->Location + bytesLength);

            MarshalEx.FillZero(fragmentPtr, diff);

            return segmentPtr;
        }
        // when new length are large than current segment must remove current item and add again
        else
        {
            // clear current block
            var dataPtr = (byte*)((nint)pagePtr + segmentPtr->Location);

            MarshalEx.FillZero(dataPtr, segmentPtr->Length);

            pagePtr->ItemsCount--;
            pagePtr->UsedBytes -= segmentPtr->Length;

            if (isLastSegment)
            {
                // if segment is end of page, must update next free location to current segment location
                pagePtr->NextFreeLocation = segmentPtr->Location;
            }
            else
            {
                // if segment is on middle of page, add content length as fragment bytes
                pagePtr->FragmentedBytes += segmentPtr->Length;
            }

            // clear slot index location/length
            segmentPtr->Location = 0;
            segmentPtr->Length = 0;

            // call insert
            return Insert(pagePtr, bytesLength, index, false);
        }
    }

    /// <summary>
    /// Defrag method re-organize all byte data content removing all fragmented data. This will move all page blocks
    /// to create a single continuous content area (just after header area). No index block will be changed (only positions)
    /// </summary>
    private void Defrag(PageMemory* pagePtr)
    {
        ENSURE(pagePtr->FragmentedBytes > 0, "do not call this when page has no fragmentation");
        ENSURE(pagePtr->HighestIndex < byte.MaxValue, "there is no items in this page to run defrag");

        // first get all blocks inside this page sorted by location (location, index)
        var blocks = new SortedList<ushort, ushort>(pagePtr->ItemsCount);

        // get first segment
        var segmentPtr = PageSegment.GetSegment(pagePtr, 0);

        // read all segments from footer
        for (ushort index = 0; index <= pagePtr->HighestIndex; index++)
        {
            // get only used index
            if (segmentPtr->Location != 0)
            {
                // sort by position
                blocks.Add(segmentPtr->Location, index);
            }

            segmentPtr--;
        }

        // here first block position
        var next = (ushort)PAGE_HEADER_SIZE;

        // now, list all segments order by location
        foreach (var slot in blocks)
        {
            var index = slot.Value;
            var location = slot.Key;

            // get segment address
            var addrPtr = PageSegment.GetSegment(pagePtr, index);

            // if current segment are not as excpect, copy buffer to right position (excluding empty space)
            if (location != next)
            {
                ENSURE(location > next, "current segment position must be greater than current empty space", new { location, next });

                // copy from original location into new (correct) location
                var sourcePtr = (byte*)((nint)pagePtr + location);
                var destPtr = (byte*)((nint)pagePtr + next);

                MarshalEx.Copy(sourcePtr, destPtr, addrPtr->Length);

                // update new location for this index (on footer page)
                addrPtr->Location = next;
            }

            next += addrPtr->Length;
        }

        // fill all non-used content area with 0
        var endContent = PAGE_SIZE - pagePtr->FooterSize;

        var nextPtr = (byte*)((nint)pagePtr + next);
        var contentLength = endContent - next;

        MarshalEx.FillZero(nextPtr, contentLength);

        // clear fragment blocks (page are in a continuous segment)
        pagePtr->FragmentedBytes = 0;
        pagePtr->NextFreeLocation = next;
    }


    /// <summary>
    /// Get a free index slot in this page
    /// </summary>
    public ushort GetFreeIndex(PageMemory* pagePtr)
    {
        // get first pointer do 0 index segment
        var segmentPtr = PageSegment.GetSegment(pagePtr, 0);

        // check for all slot area to get first empty slot [safe for byte loop]
        for (var index = 0; index <= pagePtr->HighestIndex; index++)
        {
            if (segmentPtr->Location == 0)
            {
                return (ushort)index;
            }

            segmentPtr--;
        }

        return (byte)(pagePtr->HighestIndex + 1);
    }


    /// <summary>
    /// Update HighestIndex based on current HighestIndex (step back looking for next used slot)
    /// * Used only in Delete() operation *
    /// </summary>
    private void UpdateHighestIndex(PageMemory* pagePtr)
    {
        // if current index is 0, clear index
        if (pagePtr->HighestIndex == 0)
        {
            pagePtr->HighestIndex = byte.MaxValue;
            return;
        }

        var highestIndex = (ushort)(pagePtr->HighestIndex - 1);
        var segmentPtr = PageSegment.GetSegment(pagePtr, highestIndex);

        // start from current - 1 to 0 (should use "int" because for use ">= 0")
        for (int index = highestIndex; index >= 0; index--)
        {
            if (segmentPtr->Location != 0)
            {
                pagePtr->HighestIndex = (ushort)index;
                return;
            }

            segmentPtr++;
        }

        // there is no more slots used
        pagePtr->HighestIndex = ushort.MaxValue;
    }

}
