namespace LiteDB.Engine;

[AutoInterface]
unsafe internal class BasePageModifier : IBasePageModifier
{
    protected PageSegment* Insert(PageMemory* page, ushort bytesLength, ushort index, bool isNewInsert)
    {
        ENSURE(index != ushort.MaxValue, new { bytesLength, index, isNewInsert });

        // mark page as dirty
        page->IsDirty = true;

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
        if (bytesLength > continuousBlocks)
        {
            //this.Defrag(page);
        }

        if (index > page->HighestIndex || page->HighestIndex == ushort.MaxValue)
        {
            page->HighestIndex = index;
        }

        // get segment addresses
        var segment = PageSegment.GetSegment(page, index);

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

        // create page segment based new inserted segment
        return segment;
    }

    /// <summary>
    /// Remove index slot about this page segment. Returns deleted page segment
    /// </summary>
    public void Delete(PageMemory* page, ushort index)
    {
        // mark page as dirty
        page->IsDirty = true;

        // read block position on index slot
        var segment = PageSegment.GetSegment(page, index);

        // clear both location/length
        segment->Location = 0;
        segment->Length = 0;

        // add as free blocks
        page->ItemsCount--;
        page->UsedBytes -= segment->Length;

        // clean block area with \0
        var dataPtr = (byte*)((nint)page + segment->Location); // position on page
        MarshalEx.FillZero(dataPtr, segment->Length);

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
            UpdateHighestIndex(page);
        }

        // if there is no more blocks in page, clean FragmentedBytes and NextFreePosition
        if (page->ItemsCount == 0)
        {
            ENSURE(page->HighestIndex == ushort.MaxValue, "if there is no items, HighestIndex must be clear");
            ENSURE(page->UsedBytes == 0, "should be no bytes used in clean page");

            page->NextFreeLocation = PAGE_HEADER_SIZE;
            page->FragmentedBytes = 0;
        }
    }

    /// <summary>
    /// </summary>
    protected PageSegment* Update(PageMemory* page, ushort index, ushort bytesLength)
    {
        ENSURE(bytesLength > 0, "Must update more than 0 bytes", new { bytesLength });

        // mark page as dirty
        page->IsDirty = true;

        // read page segment
        var segment = PageSegment.GetSegment(page, index);

        ENSURE(page->FreeBytes - segment->Length >= bytesLength, $"There is no free space in page {page->PageID} for {bytesLength} bytes required (free space: {page->FreeBytes}");

        // check if current segment are at end of page
        var isLastSegment = (segment->EndLocation == page->NextFreeLocation);

        // best situation: same length
        if (bytesLength == segment->Length)
        {
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
            return Insert(page, bytesLength, index, false);
        }
    }

    /// <summary>
    /// Defrag method re-organize all byte data content removing all fragmented data. This will move all page blocks
    /// to create a single continuous content area (just after header area). No index block will be changed (only positions)
    /// </summary>
    private void Defrag(PageMemory* page)
    {
        ENSURE(page->FragmentedBytes > 0, "do not call this when page has no fragmentation");
        ENSURE(page->HighestIndex < byte.MaxValue, "there is no items in this page to run defrag");

        // first get all blocks inside this page sorted by location (location, index)
        var blocks = new SortedList<ushort, ushort>(page->ItemsCount);

        // get first segment
        var segment = PageSegment.GetSegment(page, 0);

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
            var addr = PageSegment.GetSegment(page, index);

            // if current segment are not as excpect, copy buffer to right position (excluding empty space)
            if (location != next)
            {
                ENSURE(location > next, "current segment position must be greater than current empty space", new { location, next });

                // copy from original location into new (correct) location
                var sourcePtr = (byte*)((nint)page + location);
                var destPtr = (byte*)((nint)page + next);

                MarshalEx.Copy(sourcePtr, destPtr, addr->Length);

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
    private void UpdateHighestIndex(PageMemory* page)
    {
        // if current index is 0, clear index
        if (page->HighestIndex == 0)
        {
            page->HighestIndex = byte.MaxValue;
            return;
        }

        var highestIndex = (ushort)(page->HighestIndex - 1);
        var segment = PageSegment.GetSegment(page, highestIndex);

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
